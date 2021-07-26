using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MySqlDesktop
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TableForm());
            return;

            //var connectionString = "Data Source=localhost;User Id=root;Password=tp0506r1892;Port=40044;Database=datamodel; " +
            //                       "Convert Zero Datetime=true; Use Compression=true; Default Command Timeout=600; "   +
            //                       "Allow User Variables=True;";

            //Application.Run(new Home(connectionString));
        }
    }
}
