using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ansyl;
using MySqlDesktop.modules;

namespace MySqlDesktop.models
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Column
    {
        public string Table_Catalog { get; set; }
        public string Table_Schema { get; set; }
        public string Table_Name { get; set; }
        public string Column_Name { get; set; }
        public ulong? Ordinal_Position { get; set; }
        public string Column_Default { get; set; }
        public string Is_Nullable { get; set; }
        public string Data_Type { get; set; }
        public ulong? Character_Maximum_Length { get; set; }
        public ulong? Character_Octet_Length { get; set; }
        public ulong? Numeric_Precision { get; set; }
        public ulong? Numeric_Scale { get; set; }
        public ulong? Datetime_Precision { get; set; }
        public string Character_Set_Name { get; set; }
        public string Collation_Name { get; set; }
        public string Column_Type { get; set; }
        public string Column_Key { get; set; }
        public string Extra { get; set; }
        public string Privileges { get; set; }
        public string Column_Comment { get; set; }
    };

    static class ModelExtension
    {
        /// <summary>
        /// Maps MYSQL data type to .net clr type
        /// </summary>
        public static string GetDataType(string dataType, string columnType, bool isNullable)
        {
            if (dataType.In("varchar", "text", "tinytext", "mediumtext", "longtext")) return "string";

            var isUnsigned = columnType.IsMatch("unsigned");
            var appendage = isNullable ? "?" : "";

            if (columnType == "tinyint(1)") return "bool" + appendage;
            if (columnType == "char(1)") return "char" + appendage;

            if (dataType == "enum") return "byte" + appendage;
            if (dataType == "decimal") return "decimal" + appendage;
            if (dataType == "double") return "double" + appendage;
            if (dataType == "float") return "float" + appendage;

            if (dataType == "char") return "string";

            if (dataType == "tinyint") return (isUnsigned ? "byte" : "sbyte") + appendage;
            if (dataType == "smallint") return (isUnsigned ? "ushort" : "short") + appendage;
            if (dataType == "mediumint") return (isUnsigned ? "uint" : "int") + appendage;
            if (dataType == "int") return (isUnsigned ? "uint" : "int") + appendage;
            if (dataType == "bigint") return (isUnsigned ? "ulong" : "long") + appendage;

            if (dataType.In("date", "datetime", "time", "timestamp")) return "DateTime" + appendage;
            if (dataType.In("tinyblob", "blob", "longblob")) return "byte[]";

            if (dataType == "set") return "byte" + appendage;
            if (dataType == "year") return "byte" + appendage;

            //  custom types
            if (dataType == columnType) return dataType.Trim('\r', '\n');

            return null;
        }

        public static string GetDataType(this Column column)
        {
            return GetDataType(column.Data_Type, column.Column_Type, column.Is_Nullable.Equals("YES"));
        }

        //public static string Tabify(this string str)
        //{
        //    return string.Join(Environment.NewLine, from ii in str.Replace("\r\n", "\n").Split('\n')
        //                                            select '\t' + ii);
        //    return "\t" + str.Replace("\n", "\n\t");
        //}

        //public static List<string> Tabify(this IList<string> lines)
        //{
        //    return (from line in lines
        //            select "\t" + line).ToList();
        //}

        //public static List<string> Tab(this IEnumerable<string> lines)
        //{
        //    return lines.Select(line => "\t" + line).ToList();
        //}

        //public static List<string> Grp(this IEnumerable<string> lines, string postFix = null)
        //{
        //    var items = new List<string>();
        //    items.Insert(0, "{");
        //    items.AddRange(lines);
        //    items.Add("}" + postFix);
        //    return items;
        //}
    }

    public class Property
    {
        public Property(Column column)
        {
            Column = column;
            Name = column.Column_Name.ReplaceX("^[a-z]{1}", m => m.Value.ToUpper());
            Type = column.GetDataType();
        }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public Column Column { get; private set; }
    }

    public class StringList : IEnumerable<string>
    {
        readonly List<string> _lines = new List<string>();

        public void Add(int tabs, string str = null)
        {
            str = str ?? "";

            for (var i = 0; i < tabs; i++)
                str = "\t" + str;

            _lines.Add(str);
        }

        public void Add(int tabs, string format, params object[] items)
        {
            var str = format == null ? string.Empty : format.Fmt(items);

            Add(tabs, str);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _lines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Modeller for TABLES
    /// </summary>
    public class TableModel
    {
        public string Name { get; private set; }
        public Column PrimaryKey { get; private set; }
        public IList<Property> Properties { get; private set; }

        public TableModel(IList<Column> columns)
        {
            var pk = (from r in columns
                      where r.Column_Name.Equals(r.Table_Name + "Id", StringComparison.CurrentCultureIgnoreCase)
                      select r).SingleOrDefault();

            Name = pk != null
                       ? pk.Column_Name.ReplaceX("Id$", "")
                       : columns.Select(i => i.Table_Name).Distinct().Single();

            PrimaryKey = pk;
            Name = Name.ReplaceX("^[a-z]{1}", m => m.Value.ToUpper());
            Properties = columns.Select(i => new Property(i)).ToList();
        }

        public string CreateTableModel()
        {
            var items = new StringList();

            items.Add(0, PrimaryKey != null ? "public class {0} : DataObject" : "public class {0}", Name);
            items.Add(0, "{");

            foreach (var p in Properties)
            {
                items.Add(1, "public {0} {1} {{ get; set; }}", p.Type, p.Name);
            }
            items.Add(0, "}");

            return string.Join(Environment.NewLine, items);
        }

        public IEnumerable<string> CreateRowExtensionMethod()
        {
            var items = new StringList();

            items.Add(0, "public static {0} Get{0}(this DataRow row)", Name);
            items.Add(0, "{");
            items.Add(1, "if (row == null) return null;");
            items.Add(1);
            items.Add(1, "return new {0}", Name);
            items.Add(2, "{");

            foreach (var p in Properties)
            {
                items.Add(3, "{0} = row.Field<{1}>(\"{0}\"),", p.Name, p.Type);
            }

            items.Add(2, "};");
            items.Add(0, "}");

            return items;
            //return string.Join(Environment.NewLine, items);
        }

        public string CreateDataParameters()
        {
            var items = new StringList();

            items.Add(0, "public static DbParameter[] GetParameters(this {0} it)", Name);
            items.Add(0, "{");
            items.Add(1, "if (it == null) return null;");
            items.Add(1);
            items.Add(1, "return new[]");
            items.Add(2, "{");

            foreach (var p in Properties)
            {
                items.Add(3, "new DbParameter(\"@{0}\", it.{0}),", p.Name);
            }

            items.Add(2, "};");
            items.Add(0, "}");

            return string.Join(Environment.NewLine, items);
        }

        public static TableModel GetTableModel(string database, string tablename)
        {
            return GetTableModels(database, tablename).FirstOrDefault();
        }

        public static IList<TableModel> GetTableModels(string database, string tablename = null)
        {
            const string sql2 = @"
DROP TABLES IF EXISTS cc;

CREATE TEMPORARY TABLE cc
SELECT  * 
FROM    information_schema.columns
WHERE   table_schema=@table_schema AND (table_name=@table_name OR @table_name IS NULL);

ALTER TABLE cc ADD UNIQUE KEY(table_schema, table_name, ordinal_position);
ALTER TABLE cc ADD UNIQUE KEY(table_schema, table_name, column_name);

SELECT  *
FROM    cc
ORDER BY table_schema, table_name, ordinal_position";

            var parameters = new { table_schema = database, table_name = tablename };

            return (from c in DbConnection.Query<Column>(sql2, parameters)
                    group c by c.Table_Name
                    into g
                    select new TableModel(g.ToList())).ToList();
        }
    }

    public class Modeller
    {
        /// <summary>
        /// Exports generated code to a File
        /// </summary>
        /// <param name="SelectedDatabase"></param>
        /// <returns></returns>
        public static string CopyAll(string SelectedDatabase)
        {
            var sql = "SELECT table_name FROM information_schema.tables tb WHERE tb.table_schema=@table_schema";

            var tables = DbConnection.Query<string>(sql, new { table_schema = SelectedDatabase });

            var sw = new StringWriter();

            sw.WriteLine("namespace {0}", SelectedDatabase);
            sw.WriteLine("{");

            foreach (var li in tables)
            {
                var tableModel = TableModel.GetTableModel(SelectedDatabase, li);
                var code = tableModel.CreateTableModel();
                sw.WriteLine();
                sw.WriteLine(code);
            }
            sw.WriteLine("}");

            var str = sw.ToString();

            //Clipboard.SetText(str);

            File.WriteAllText(SelectedDatabase + "_database_model.txt", str);

            return str;
        }
    }
}