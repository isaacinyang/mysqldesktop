using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySqlDesktop.data.datamodel;

namespace MySqlDesktop.uow
{
    public class DatabaseManager : BaseExporter
    {
        private readonly string _database;
        private readonly IList<Entity> _entities;
        private readonly IList<TableManager> _tableManagers;

        public DatabaseManager(string database)
        {
            _database = database;
            _entities = OneTask<Entity>.List(e => e.Database == database);

            //  folder
            {
                CreateOutputFolder(_database);

                foreach (var fi in Directory.GetFiles(outputFolder, "*.*", SearchOption.AllDirectories))
                {
                    File.Delete(fi);
                }
            }

            var records = OneTask<Record>.List(e => e.Database == database);

            _tableManagers = (from et in _entities
                              let rc = records.Where(i => i.Table == et.Table).ToList()
                              let tm = TableManager.Get(database, et, rc)
                              where tm != null
                              select tm).ToList();
        }

        private void ExportOneTransactionExtension()
        {
            var methods = new List<string>();

            //  1
            foreach (var tp in new[] { "long", "int", "short", "byte" })
            {
                var leader = $"public static T GetDto<T>(this OneTransaction ot, {tp} id) where T : DataObject";
                var tpConvert = tp == "long" ? "return ot.Get<T>((int) id);" : "return ot.Get<T>(id);";
                var str = GroupCode.Get(leader, tpConvert);
                methods.Add(str);
            }

            //  2
            {
                var leader = "public static long InsertOrUpdate<T>(this OneTransaction ot, T entity) where T : DataObject, IDataEntity";

                var str = GroupCode.Get(leader,
                                        "var id = entity.GetPrimaryId();",
                                        "if (id <= 0) return ot.Insert(entity);",
                                        "",
                                        "ot.Update(entity);",
                                        "return id;");
                methods.Add(str);
            }

            //  3
            foreach (var et in _entities)
            {
                var dataModel = $"{et.ClassName}";
                var viewModel = $"{et.ClassName}ViewModel";
                var methodName = $"Get{dataModel}";

                var str1 = $"public static {viewModel} GetViewModel(this OneTransaction ot, {dataModel} it, bool isLazy = true)";
                var str2 = $"\t=> ViewModeller.{methodName}(ot, it, isLazy);";
                methods.Add(string.Join(Environment.NewLine, str1, str2));
            }

            string code = null;

            //  4
            {
                var leader = "public static partial class OneTransactionExtension";
                code = SectionCode.Get(leader, methods);
            }

            //  5
            {
                var leaders = new List<string>
                              {
                                  @"using ansyl.dao;",
                                  $"using dm.{_database};",
                                  //$"using vm.{_database};",
                                  $"using view.{_database};",
                                  $"",
                                  $"namespace dto.{_database}"
                              };

                var str = GroupCode.Get(leaders, new[] { code });

                WriteAllText("dto\\OneTransactionExtension.cs", str);
            }
        }

        private void ExportDtoExtension()
        {
            var section1 = Section.Two()
                                  .AddLeader("public static class DtoExtension")
                                  .AddChildren(_tableManagers.Select(tm => tm.GetApplyViewModel()))
                                  .AddChildren(_tableManagers.Select(tm => tm.GetSaveMethod()));

            var fileLeader = @$"
using dm.{_database};
using view.{_database};

namespace dto.{_database}".Trim('\r', '\n');

            var section = Section.Two()
                                 .AddLeader(fileLeader)
                                 .AddChild(section1)
                                 .ToString();

            WriteAllText("dto\\DtoExtension.cs", section);
        }

        private void ExportDtoSaveExtension()
        {
            string code = null;

            {
                const string leader = "public static class DtoExtension";

                var methods = (from tm in _tableManagers
                               select tm.GetApplyViewModel()).ToList();

                code = SectionCode.Get(leader, methods);
            }

            {
                var leaders = new List<string>();

                leaders.Add($"using dm.{_database};");
                leaders.Add($"using view.{_database};");
                leaders.Add($"");
                leaders.Add($"namespace dto.{_database}");

                var str = GroupCode.Get(leaders, new[] { code });

                WriteAllText("dto\\DtoExtension.cs", str);
            }
        }

        public string ExportAll()
        {
            ExportDataModels();
            ExportViewModels();
            ExportDataToViewModeller();
            //ExportViewToDataConverter();
            ExportEnumSetTypes();
            ExportWebApiMethods();
            ExportMvcActionMethods();
            ExportCsStyleSheet();
            ExportCsHtmlViews();
            ExportFkText();
            ExportEtClasses();
            ExportDtoExtension();
            ExportOneTransactionExtension();

            return outputFolder;
        }

        //  1: data modeller
        private void ExportDataModels()
        {
            //var sw = new StringWriter();

            //sw.WriteLine("//ReSharper disable CheckNamespace");
            //sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
            //sw.WriteLine($"using System;");
            //sw.WriteLine($"using ansyl.dao;");
            //sw.WriteLine($"using dm.{_database}.types;");
            //sw.WriteLine($"using dto;");
            //sw.WriteLine();
            //sw.WriteLine($"namespace dm.{_database}");

            //var parts = _tableManagers.Select(m => m.GetDataModelClass());
            //sw.WriteLine(CodeGroup(parts));

            //WriteAllText($"dao\\{_database}_datamodels.cs", sw);

            var section = Section.Two();

            section.AddLeader("//ReSharper disable CheckNamespace")
                             .AddLeader($"//Creation Time: {DateTime.Now:s}")
                             .AddLeader($"using System;")
                             .AddLeader($"using ansyl.dao;")
                             .AddLeader($"using dm.{_database}.types;")
                             .AddLeader($"using dto;")
                             .AddLeader("")
                             .AddLeader($"namespace dto.{_database}");

            //var section1 = Section.New(SectionType.Line)
            //                      .AddLeader("public interface IDataEntity")
            //                      .AddChild("long GetPrimaryId();")
            //                      .ToString();
            //section.AddChild(section1);

            section.AddChildren(_tableManagers.Select(tm => tm.GetDataModelClass()));

            WriteAllText($"dao\\{_database}_datamodels.cs", section.ToString());
        }

        //  2: view modeller
        private void ExportViewModels()
        {
            ////  abstract base class
            //var sw = new StringWriter();
            //sw.WriteLine("public abstract class BaseViewModel");
            //sw.Write(CodeGroup(string.Empty));

            //var parts = new List<string>();
            //parts.Add(sw.ToString());
            //parts.AddRange(from tm in _tableManagers
            //               select tm.GetViewModelClass());

            //sw = new StringWriter();
            //sw.WriteLine("//ReSharper disable CheckNamespace");
            //sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
            //sw.WriteLine();
            //sw.WriteLine($"using dm.{_database};");
            //sw.WriteLine($"using dm.{_database}.types;");
            //sw.WriteLine();
            //sw.WriteLine($"namespace view.{_database}");
            //sw.WriteLine(CodeGroup(parts));


            var section1 = Section.One()
                                  .AddLeader("public abstract class BaseViewModel")
                                  .ToString();

            var section = Section.Two();

            section.AddLeader("//ReSharper disable CheckNamespace")
                   .AddLeader($"//Creation Time: {DateTime.Now:s}")
                   //.AddLeader($"using System;")
                   //.AddLeader($"using ansyl.dao;")
                   .AddLeader($"using dm.{_database};")
                   .AddLeader($"using dm.{_database}.types;")
                   //.AddLeader($"using dto;")
                   .AddLeader("")
                   .AddLeader($"namespace view.{_database}");

            section.AddChild(section1);
            section.AddChildren(_tableManagers.Select(tm => tm.GetViewModelClass()));


            WriteAllText($"dao\\{_database}_viewmodels.cs", section.ToString());
        }

        //  3: data => view converter
        private void ExportDataToViewModeller()
        {
            var sections = new[]
                           {
                               _tableManagers.Select(tm => tm.GetDataToView1()),
                               _tableManagers.Select(tm => tm.GetDataToView2()),
                               _tableManagers.Select(tm => tm.GetDataToView3()),
                               _tableManagers.Select(tm => tm.GetDataToView4())
                           };

            //var str = GroupCode.Get($"static string GetText<T>(Fkk fk, int? id) where T : DataObject",
            //                        $"return fk == null || id == null ? null : fk.GetText<T>(id);");

            //sections.Insert(0, str);

            var group1 = SectionCode.Get($"public partial class ViewModeller", from section in sections
                                                                               from line in section
                                                                               select line);

            var leaders = new List<string>
                              {
                                  "//ReSharper disable CheckNamespace",
                                  $"//Creation Time: {DateTime.Now:s}",
                                  $"using dm.{_database};",
                                  $"using ansyl.dao;",
                                  $"using dto;",
                                  $"using dto.{_database};",
                                  "",
                                  $"namespace view.{_database}"
                              };

            var fileContent = SectionCode.Get(leaders, new[] { group1 });

            WriteAllText($"dao\\{_database}_viewModeller.cs", fileContent);
        }

#if Dropped
        private static string GetCodeGroup(IEnumerable<string> leaders, IEnumerable<string> children)
        {
            var sw = new StringWriter();

            foreach (var leader in leaders)
            {
                sw.WriteLine(leader);
            }

            sw.WriteLine(CodeGroup(children));

            return sw.ToString().TrimEnd('\r', '\n', '\t', ' ');
        }

        private void ExportDataToViewConverter1()
        {
            var sections = _tableManagers.Select(tm => tm.GetDataToView1()).ToList();

            var str = GroupCode.Get($"static string GetText<T>(Fkk fk, int? id) where T : DataObject",
                                    $"return fk == null || id == null ? null : fk.GetText<T>(id);");

            sections.Insert(0, str);

            ExportDataToViewConverterN(1, sections);
        }

        private void ExportDataToViewConverter2()
        {
            ExportDataToViewConverterN(2, _tableManagers.Select(tm => tm.GetDataToView2()));
        }

        private void ExportDataToViewConverter3()
        {
            ExportDataToViewConverterN(3, _tableManagers.Select(tm => tm.GetDataToView3()));
        }

        private void ExportDataToViewConverter4()
        {
            ExportDataToViewConverterN(4, _tableManagers.Select(tm => tm.GetDataToView4()));
        }

        private void ExportDataToViewConverterN(int no, IEnumerable<string> sections)
        {
            var group1 = SectionCode.Get($"public partial class ViewModeller",
                                         sections);

            var leaders = new List<string>
                          {
                              "//ReSharper disable CheckNamespace",
                              $"//Creation Time: {DateTime.Now:s}",
                              $"using dm.{_database};",
                              $"using ansyl.dao;",
                              $"using dto;",
                              "",
                              $"namespace view.{_database}"
                          };

            var fileContent = SectionCode.Get(leaders, new[] { group1 });

            WriteAllText($"dao\\{_database}_view_modeller_{no}.cs", fileContent);
        }

        private void ExportViewToDataConverter()
        {
            return;

            var parts = _tableManagers.Select(m => m.GetViewToData());
            var str = TabMergedParts(parts);
            str = Tabify(str);

            var sw = new StringWriter();

            sw.WriteLine("//ReSharper disable CheckNamespace");
            sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
            sw.WriteLine($"using dm.{_database};");
            sw.WriteLine($"using dto;");
            sw.WriteLine();
            sw.WriteLine($"namespace vm.{_database}");
            sw.WriteLine($"{{");
            sw.WriteLine($"\tpublic class Modeller");
            sw.WriteLine($"\t{{");
            sw.WriteLine(str);
            sw.WriteLine($"\t}}");
            sw.WriteLine($"}}");

            WriteAllText($"dao\\{_database}_view_to_data.cs", sw);
        }
#endif

        //  5: enum & set types
        private void ExportEnumSetTypes()
        {
            var str = _tableManagers.FirstOrDefault()?.GetEnumSetTypes();

            var sw = new StringWriter();

            sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
            sw.WriteLine();
            sw.WriteLine(str);

            WriteAllText($"dao\\{_database}_enum_set_types.cs", sw);
        }

        public static string GetFileContent(string filename)
        {
            var fi = new FileInfo(filename);

            if (fi.Exists)
                return File.ReadAllText(fi.FullName);

            return string.Empty;
        }

        //  6: web api method
        private void ExportWebApiMethods()
        {
            var section1 = Section.One()
                                  .AddLeader("protected IHttpActionResult Send(Func<IHttpActionResult> fn)")
                                  .AddChild(Section.One()
                                                   .AddLeader("try")
                                                   .AddChild("return fn.Invoke();"))
                                  .AddChild(Section.One()
                                                   .AddLeader("catch (Exception exception)")
                                                   .AddChild("return Content(HttpStatusCode.InternalServerError, exception.ToString());"))
                                  .ToString();

            var section2 = Section.Two()
                                 .AddLeader("public class DaoController : ApiController")
                                 .AddChild(section1);

            //  2. WebApiActions
            var content = GetFileContent("Files\\WebApiActions.txt").TrimEnd('\t', '\n', '\r', ' ');

            foreach (var tableManager in _tableManagers)
            {
                var str1 = content.Replace("{Entity}", tableManager.Entity.ClassName);

                section2.AddChild(str1);
            }

            var leader = @"
// ReSharper disable CheckNamespace
using System;
using System.Net;
using System.Web.Http;

namespace dto.apiControllers";

            var section = Section.Two()
                                 .AddLeader(leader)
                                 .AddChild(section2)
                                 .ToString();

            WriteAllText($"mvc\\{_database}_api_DaoController.cs", section);
            return;

            //  1. VmApiController
            var sourceFile = "Files\\VmApiController.txt";
            var targetFile = $"mvc\\VmApiController.cs";

            var sw = new StringWriter();

            sw.WriteLine(File.ReadAllText(sourceFile));
            WriteAllText(targetFile, sw);

            var parts = _entities.Select(et => content.Replace("{Entity}", et.ClassName));
            var str = TabMergedParts(parts);
            str = Tabify(str);

            sw = new StringWriter();

            sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
            sw.WriteLine($"using System.Web.Http;");
            sw.WriteLine();
            //sw.WriteLine($"namespace vm.apiControllers.{_database}");
            sw.WriteLine($"namespace vm.apiControllers");
            sw.WriteLine($"{{");
            sw.WriteLine($"\tpublic class DaoController : VmApiController");
            sw.WriteLine($"\t{{");
            sw.WriteLine(str);
            sw.WriteLine($"\t}}");
            sw.WriteLine($"}}");

            WriteAllText($"mvc\\{_database}_api_DaoController.cs", sw);
        }

        //  7: mvc controller action methods
        private void ExportMvcActionMethods()
        {
            //  1. MVC Controller Actions
            var content = GetFileContent("Files\\ActionResults.txt");

            var parts = _entities.Select(et => content.Replace("{Entity}", et.ClassName));
            var str = TabMergedParts(parts);
            str = Tabify(str);

            var sw = new StringWriter();

            sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
            sw.WriteLine();
            sw.WriteLine($"using System.Web.Http;");
            sw.WriteLine();
            sw.WriteLine($"namespace vm.Controllers");
            sw.WriteLine($"{{");
            sw.WriteLine($"\tpublic class DaoController : Controller");
            sw.WriteLine($"\t{{");
            sw.WriteLine(str);
            sw.WriteLine($"\t}}");
            sw.WriteLine($"}}");

            WriteAllText($"mvc\\{_database}_mvc_DaoController.cs", sw);
        }

        //  8: CSS Style Sheet
        private void ExportCsStyleSheet()
        {
            var sourceFile = "Files\\Css.txt";
            var targetFile = $"styles\\viewmodel.css";

            var sw = new StringWriter();
            sw.WriteLine(File.ReadAllText(sourceFile));

            WriteAllText(targetFile, sw);
        }

        //  9: CSHTML views
        private void ExportCsHtmlViews()
        {
            foreach (var mgr in _tableManagers)
            {
                WriteAllText($"views_dv\\{mgr.DmClass}View_DV.cshtml", mgr.Ui.GetViewOneDV());
                WriteAllText($"views_dl\\{mgr.DmClass}View_DL.cshtml", mgr.Ui.GetViewOneDL());
                WriteAllText($"views_dv\\{mgr.DmClass}Edit_DV.cshtml", mgr.Ui.GetEditOneDV());
                WriteAllText($"views_dl\\{mgr.DmClass}Edit_DL.cshtml", mgr.Ui.GetEditOneDL());
            }
        }

        //  10a: Foreign Key Representation
        private void ExportFkText()
        {
            //new FkExport(_database, _entities).Export();
            new FkkExport(_database, _entities).Export();
        }

        ////  10b: Foreign Key Representation
        //public void ExportFkkText()
        //{
        //}

        //  11: 
        private void ExportEtClasses()
        {
            var dic = new Dictionary<string, string>();

            dic.Add("Html.txt", "Html.cs");
            dic.Add("DaoFactory.txt", "DaoFactory.cs");
            //dic.Add("DataModeller.txt", "DataModeller.cs");
            dic.Add("Dt.txt", "Dt.cs");

            foreach (var (key, value) in dic)
            {
                var sourceFile = $"Files\\{key}";
                var targetFile = $"dao\\{value}";

                var sw = new StringWriter();
                sw.WriteLine("//ReSharper disable CheckNamespace");
                sw.WriteLine(File.ReadAllText(sourceFile));

                WriteAllText(targetFile, sw);
            }
        }
    }
}
