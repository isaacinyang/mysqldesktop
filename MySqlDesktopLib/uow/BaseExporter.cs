using System;
using System.Collections.Generic;
using System.IO;
using ansyl;

namespace MySqlDesktop.uow
{
    public abstract class BaseExporter
    {
        protected static string outputFolder = null;

        protected static void CreateOutputFolder(string _database)
        {
            //  get output folder
            var currentFolder = Directory.GetCurrentDirectory();

            var dirPath = Path.Combine(new DirectoryInfo(currentFolder).Root.FullName, "mysqldesktop", "codegen", _database);

            foreach (var dirname in new[] {null, "views_dl", "views_dv", "styles", "mvc", "dao"})
            {
                var dir = dirname == null ? dirPath : Path.Combine(dirPath, dirname);

                if (Directory.Exists(dir) == false)
                    Directory.CreateDirectory(dir);
            }

            outputFolder = dirPath;
        }

        protected static string MergeParts(IEnumerable<string> parts)
        {
            return string.Join(Environment.NewLine, parts)
                         .TrimEnd('\r', '\n', '\t', ' ');
        }

        protected static string Tabify(string s)
        {
            return s.TrimEnd('\r', '\n', '\t')
                    .ReplaceX(@"(^)|((\r)?(\n))", m => m.Value + "\t");
        }

        protected static string Tabify(StringWriter sw)
        {
            return Tabify(sw.ToString());
        }

        protected static string TabMergedParts(IEnumerable<string> parts)
        {
            return Tabify(MergeParts(parts));
        }

        public static string CodeGroup(string str)
        {
            var swOut = new StringWriter();
            swOut.WriteLine($"{{");

            if (str.IsNullOrWhite() == false)
                swOut.WriteLine(Tabify(str));

            swOut.WriteLine($"}}");
            return swOut.ToString().TrimEnd('\r', '\n');
        }

        static readonly string NewLine = Environment.NewLine;

        protected static string CodeGroup(IEnumerable<string> lines)
        {
            //var str = string.Join(NewLine, lines);
            var str = string.Join("\r\n\r\n", lines);
            return CodeGroup(str);
        }

        protected void WriteAllText(string filename, StringWriter sw) => WriteAllText(filename, sw.ToString());

        protected void WriteAllText(string filename, string str)
        {
            var filePath = Path.Combine(outputFolder, filename);
            var fi = new FileInfo(filePath);

            var di = fi.Directory;
            if (di != null && di.Exists == false)
                di.Create();

            File.WriteAllText(filePath, str);
        }
    }
}
