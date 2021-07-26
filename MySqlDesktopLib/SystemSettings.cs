using System.IO;
using System.Linq;

namespace MySqlDesktop
{
    public class SystemSettings
    {
        static SystemSettings()
        {
            Get();
        }

        private const string FilePath = "systemSettings.txt";

        public static void Set(string database, string table, string field = null)
        {
            database ??= string.Empty;
            table    ??= string.Empty;
            field    ??= string.Empty;

            var sw = new StringWriter();
            sw.WriteLine($"Database={database}");
            sw.WriteLine($"Table={table}");
            sw.WriteLine($"Field={field}");
            File.WriteAllText(FilePath, sw.ToString());
        }

        public static void Get()
        {
            if (File.Exists(FilePath) == false)
            {
                Set(null, null, null);
            }

            var dic = (from line in File.ReadAllLines(FilePath)
                       let flds = line.Split('=')
                       where flds.Length == 2
                       let val = flds[1]
                       select new
                              {
                                  Key   = flds[0],
                                  Value = string.IsNullOrWhiteSpace(val) ? null : val
                              }).ToDictionary(k => k.Key, v => v.Value);

            Database = dic["Database"];
            Table    = dic["Table"];
            Field    = dic["Field"];
        }

        public static string Database { get; private set; }
        public static string Table    { get; private set; }
        public static string Field    { get; private set; }
    }
}
