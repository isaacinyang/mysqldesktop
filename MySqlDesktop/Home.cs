using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ansyl;
using ansyl.forms;
using MySql.Data.MySqlClient;
using MySqlDesktop.models;
using MySqlDesktop.modules;

namespace MySqlDesktop
{
    public partial class Home : Form
    {
        private readonly string _connectionString;
        private bool _isLoaded;
        private string SelectedDatabase => lboDatabase.Text;
        private string SelectedTable => lboTable.Text;

        public Home(string connectionString)
        {
            _connectionString = connectionString;
            SqlX.ConnString = connectionString;
            InitializeComponent();
        }

        private void Home_Load(object sender, EventArgs e)
        {
            //DbConnection.ConnectionString = _connectionString;
            toolStripStatusLabel1.Text = _connectionString;

            btnModeller.Click += BtnModeller_Click;
            lboDatabase.SelectedIndexChanged += LboDatabase_SelectedIndexChanged;
            lboTable.SelectedIndexChanged += LboTable_SelectedIndexChanged;

            lboDatabase.BindTo(BLL.GetDatabases());
            lboDatabase.Text = SystemSettings.Database;

            tabPage1.Text = "C# Entity Class";
            tabPage2.Text = "Field Definition";
            tabPage3.Text = "Table Sizes";

            _isLoaded = true;
        }

        private void BtnModeller_Click(object sender, EventArgs e)
        {
            new TableForm().ShowDialog(this);
        }

        private void LboTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView2.DataSource = BLL.GetFields(lboDatabase.Text, lboTable.Text);
            SystemSettings.Set(SelectedDatabase, SelectedTable);

            var tableModel = TableModel.GetTableModel(SelectedDatabase, SelectedTable);
            var code = tableModel.CreateTableModel();
            richTextBox1.Text = code;
            CopyText(code);
        }

        static void CopyText(string text)
        {
            Clipboard.SetText(text);
        }

        private void LboDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tables = BLL.GetTableStatuses(lboDatabase.Text);
            lboTable.BindTo(tables.Select(t => t.Name));
            lboTable.Text = SystemSettings.Table;

            dataGridView1.DataSource = tables;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.ValueType == typeof(ulong))
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    column.DefaultCellStyle.Format = "n0";
                }
            }
        }

        private void btnCopyAll_Click(object sender, EventArgs e)
        {
            Modeller.CopyAll(lboDatabase.Text);
        }
    }
}
