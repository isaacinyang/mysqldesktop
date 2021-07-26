using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using ansyl;
using MySqlDesktop.data.datamodel;
using MySqlDesktop.ui;

namespace MySqlDesktop.uow
{
    public class TableManager
    {
        public Entity        Entity  { get; }
        public string        DmClass { get; }
        public string        VmClass { get; }
        public TableUi       Ui      { get; }
        public IList<Record> Records { get; }

        public static TableManager Get(string database, string table)
        {
            var entity  = OneTask<Entity>.Get(e => e.Database  == database && e.Table == table);
            var records = OneTask<Record>.List(e => e.Database == database && e.Table == table);

            return Get(database, entity, records);
        }

        public static TableManager Get(string database, Entity entity, IList<Record> records)
        {
            return entity == null || records == null || records.Count == 0
                       ? null
                       : new TableManager(database, entity, records);
        }

        TableManager(string database, Entity entity, IList<Record> records)
        {
            Entity  = entity;
            DmClass = $"{entity.ClassName}";
            VmClass = $"{entity.ClassName}ViewModel";
            Records = records;
            Ui      = new TableUi(database, Entity, Records);
        }

        public string GetViewModelClass()
        {
            var lines = Records.Select(i => i.ViewModelCode).ToList();
            lines.AddRange(Records.Select(i => i.ViewModelFKCode).Where(c => c != null));
            //lines.Add($"public int GetPrimaryId() => {DmClass}Id;");

            return GroupCode.Get($"public class {VmClass} : BaseViewModel", //, IViewEntity
                                 from r in lines
                                 where r != null
                                 select r);
        }

        public string GetDataModelClass()
        {
            var records = Records.Select(r => r.DataModelCode).ToList();
            records.Add($"public long GetPrimaryId() => {DmClass}Id;");

            return GroupCode.Get($"public class {DmClass} : DataObject, IDataEntity",
                                 records);
        }

        public string GetViewToData()
        {
            var entityId = $"{DmClass}Id";

            var lines = new List<string>
                        {
                            $"if (dm == null || dm.{entityId} != vm.{entityId} || dm.{entityId} == 0)",
                            $"\tdm = new {DmClass}();"
                        };

            foreach (var r in from r in Records
                              let v = r.ViewToData
                              where v != null
                              select v.Trim(',', ' '))
            {
                lines.Add($"dm.{r};");
            }

            lines.Add($"");
            lines.Add($"return dm;");

            return GroupCode.Get($"public static {DmClass} GetDataModel({VmClass} vm, {DmClass} dm = null)", lines);
        }

        public string GetDataToView()
        {
            return string.Join("\r\n\r\n", GetDataToView1(), GetDataToView2(), GetDataToView3(), GetDataToView4());
        }

        public string GetDataToView1()
        {
            var methodName = $"Get{DmClass}";

            var codeLines = new List<string>();
            codeLines.AddRange(Records.Select(i => i.DataToView));
            codeLines.AddRange(Records.Select(i => i.DataToViewFK));

            var str1 = GroupCode.Get($"return new {VmClass}", from c in codeLines
                                                              where c.IsNullOrWhite() == false
                                                              select c) + ";";

            return SectionCode.Get($"public static {VmClass} {methodName}(OneTransaction ot, {DmClass} dm, bool isLazy = true)",
                                   $"if (dm == null) return new {VmClass}();",
                                   $"var fk = isLazy ? null : new Fkk(ot);",
                                   str1);
        }

        public string GetDataToView2()
        {
            var methodName = $"Get{DmClass}";

            //var code1 = GroupCode.Get($"using (var ot = isLazy ? null : new OneTransaction())",
            //                          $"return {methodName}(ot, dm, isLazy);");

            return GroupCode.Get($"public static {VmClass} {methodName}({DmClass} dm, bool isLazy = true)",
                                 $"using var ot = isLazy ? null : new OneTransaction();",
                                 $"return {methodName}(ot, dm, isLazy);");
        }

        public string GetDataToView3()
        {
            var methodName = $"Get{DmClass}";

            return GroupCode.Get($"public static {VmClass} {methodName}(OneTransaction ot, int id, bool isLazy = true)",
                                 $"var dm = ot.Get<{DmClass}>(id);",
                                 //$"var dm = DataModeller.Get<{DmClass}>(ot, id);",
                                 $"return {methodName}(ot, dm, isLazy);");
        }

        public string GetDataToView4()
        {
            var methodName = $"Get{DmClass}";

            //var code1 = GroupCode.Get($"using (var ot = new OneTransaction())",
            //                          $"var dm = DataModeller.Get<{DmClass}>(ot, id);",
            //                          $"return {methodName}(ot, dm, isLazy);");

            return GroupCode.Get($"public static {VmClass} {methodName}(int id, bool isLazy = true)",
                                 $"using var ot = new OneTransaction();",
                                 $"var dm = ot.Get<{DmClass}>(id);",
                                 //$"var dm = DataModeller.Get<{DmClass}>(ot, id);",
                                 $"return {methodName}(ot, dm, isLazy);");
        }

        public string GetEnumSetTypes()
        {
            const char separator = 'µ';

            var records = (from rc in OneTask<Record>.List(e => e.Database == Entity.Database &&
                                                                (e.DataType == "enum" || e.DataType == "set"))
                           group rc by new {rc.Column, rc.ColumnType}
                           into g
                           select g.Key).ToList();

            var sw = new StringWriter();

            var indent = 0;

            //  functions
            string Tabs(int n) => string.Join("", Enumerable.Repeat("\t", n));
            void WriteLine(string str) => sw.WriteLine($"{Tabs(indent)}{str}");

            WriteLine($"using System.ComponentModel;");
            WriteLine($"");

            //  namespace
            WriteLine($"namespace dm.{Entity.Database}.types");
            WriteLine($"{{");

            indent++;

            for (int r = 0; r < records.Count; r++)
            {
                if (r > 0)
                    WriteLine("");

                var rc     = records[r];
                var column = rc.Column;
                var type   = rc.ColumnType;

                var isEnum = type.IsMatch("^enum");
                var isSet  = type.IsMatch("^set");

                type = type.ReplaceX("','", separator.ToString())
                           .ReplaceX("^(enum|set)", "")
                           .Trim('(', '\'', ')');

                var flds = type.Split(separator);

                if (isSet)
                    WriteLine($"[Flags]");

                if (column.IsMatch("Enum"))
                    WriteLine($"public enum {column}");
                else
                    WriteLine($"public enum Enum{column}");

                WriteLine($"{{");

                for (int i = 0, v = isSet ? 0 : 1; i < flds.Length; i++, v++)
                {
                    var fld = flds[i].ReplaceX("[^a-zA-Z0-9]+", "");
                    if (fld.IsNullOrWhite() != false) continue;

                    indent++;

                    var description = Regex.Replace(fld, "[A-Z0-9]+", m => " " + m.Value).Trim();

                    if (description != fld)
                        WriteLine($"[Description(\"{description}\")]");

                    var str = isEnum ? $"{fld} = {v}" : $"{fld} = 1 << {i}";
                    WriteLine($"{str},");

                    indent--;
                }

                WriteLine($"}}");
            }

            indent--;
            WriteLine($"}}");

            return sw.ToString();
        }

        public string GetApplyViewModel()
        {
            var entity = Entity.ClassName;

            var child1 = @$"
if (dm == null && vm == null)
    return null;

if (dm == null && vm.{entity}Id > 0)
    return null;

if (dm == null && vm.{entity}Id == 0)    //  this is a new insertion
    dm = new {entity}();";

            var section = Section.One()
                                  .AddLeader($"public static {DmClass} Apply(this {DmClass} dm, {VmClass} vm)")
                                  .AddChild(child1, true);

            section.AddChildren(from r in Records
                                let v = r.ViewToData?.Trim(',', ' ')
                                where v != null
                                select $"dm.{v};");

            return section.ToString();



            var lines = new List<string>
                        {
                            $"if (dm == null && vm == null)",
                            $"\treturn null;",
                            $"",
                            $"if (dm == null && vm.{entity}Id > 0)",
                            $"\treturn null;",
                            $"",
                            $"if (dm == null && vm.{entity}Id == 0)    //  this is a new insertion",
                            $"\tdm = new {entity}();",
                            $""
                        };

            foreach (var r in from r in Records
                              let v = r.ViewToData
                              where v != null
                              select v.Trim(',', ' '))
            {
                lines.Add($"dm.{r};");
            }

            lines.Add($"");
            lines.Add($"return dm;");

            var leader = $"public static {DmClass} Apply(this {DmClass} dm, {VmClass} vm)";
            return GroupCode.Get(leader, lines);
        }

        public string GetSaveMethod()
        {
            var child1 = $@"
using var ot = new OneTransaction();
var fa = ot.GetDto<{DmClass}>(model.{DmClass}Id).Apply(model);
ot.InsertOrUpdate(fa);
ot.SaveChanges();".Trim('\r', '\n');

            return Section.One()
                          .AddLeader($"public static void Save(this {VmClass} model)")
                          .AddChild(child1)
                          .ToString();
        }
    }
}
