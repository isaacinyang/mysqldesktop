using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ansyl;
using ansyl.forms;
using Dapper;
using MySqlDesktop.data;
using MySqlDesktop.data.datamodel;
using MySqlDesktop.modules;
using MySqlDesktop.ui;
using MySqlDesktop.uow;
using DataGridViewHelper = ansyl.datagridview.DataGridViewHelper;

namespace MySqlDesktop
{
    public sealed partial class TableForm : Form
    {
        public TableForm()
        {
            InitializeComponent();
            //this.SetDialog();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.Font = new System.Drawing.Font(this.Font.Name, 12);
        }

        private IList<Entity> _entities = new List<Entity>();

        private void TableForm_Load(object sender, EventArgs e)
        {
            cboDatabase.SelectedIndexChanged += CboDatabase_SelectedIndexChanged;
            listBox1.SelectedIndexChanged += ListBox1_SelectedIndexChanged;
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

            toolStripButton1SaveAll.Click += ToolStripButton1SaveAll_Click;
            toolStripButton2ResetAll.Click += ToolStripButton2ResetAll_Click;

            dgvTableSizes.SortCompare += DgvTableSizes_SortCompare;


            SetDataSource();
            SetTabTitles();
            SetLabelTexts();
            SelectTable();

            SystemSettings.Get();

            cboDatabase.Text = SystemSettings.Database;
            listBox1.SelectedValue = SystemSettings.Table;

            dgvTableSizes.AllowUserToAddRows =
                dgvTableSizes.AllowUserToDeleteRows = false;

            _isLoaded = true;

            //Text = DateTime.Now.ToMyString();
        }

        private void SetDataSource()
        {
            //  entity table
            _entities              = OneTask<Entity>.List();
            cboDatabase.DataSource = _entities.Select(i => i.Database).Distinct().OrderBy(i=>i).ToList();
        }

        private void DgvTableSizes_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {

            // Try to sort based on the cells in the current column.
            e.SortResult = System.String.Compare(
                                                 e.CellValue1.ToString(), e.CellValue2.ToString());

            // If the cells are equal, sort based on the ID column.
            if (e.SortResult == 0 && e.Column.Name != "ID")
            {
                e.SortResult = System.String.Compare(
                                                     dgvTableSizes.Rows[e.RowIndex1].Cells["ID"].Value.ToString(),
                                                     dgvTableSizes.Rows[e.RowIndex2].Cells["ID"].Value.ToString());
            }
            e.Handled = true;

        }

        private void ToolStripButton2ResetAll_Click(object sender, EventArgs e)
        {
            using var connection = ConnectionFactory.GetConnection();
            connection.Execute("datamodel.setup", new{l_database = CurrentDatabase}, commandType: CommandType.StoredProcedure);

            var database = SystemSettings.Database;
            var table = SystemSettings.Table;

            SetDataSource();

            cboDatabase.Text       = database;
            listBox1.SelectedValue = table;

            MessageBox.Show(@"Reset Completed");
        }

        private void ToolStripButton1SaveAll_Click(object sender, EventArgs e)
        {
            var folder = new DatabaseManager(CurrentDatabase).ExportAll();
            MessageBox.Show($@"Export of {CurrentDatabase} Completed");

            Process.Start("explorer.exe", folder);
        }

        private bool _isLoaded;

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReadTabPage(tabControl1.SelectedIndex);
        }

        public void ReadTabPage(int selectedIndex)
        {
            var tabPage = tabControl1.TabPages[selectedIndex];

            //var boxes = tabPage.Controls.OfType<RichTextBox>().ToList();
            //var text = boxes.SingleOrDefault()?.Text;

            var panel = (Panel)tabPage.Controls.OfType<TableLayoutPanel>().SingleOrDefault() ?? tabPage;
            var boxes = panel.Controls.OfType<RichTextBox>().ToList();

            var sw = new StringWriter();

            for (var i = 0; i < boxes.Count; i++)
            {
                if (i > 0)
                {
                    sw.WriteLine("////////////////////////////////////////");
                    sw.WriteLine();
                }

                sw.WriteLine(boxes[i].Text);
            }

            var str = sw.ToString().Trim();
            if (str.IsNullOrWhite())
                Clipboard.Clear();
            else
                Clipboard.SetText(str);

            toolStripStatusLabel1.Text = $"{boxes.Count} @ {DateTime.Now}";
        }

        private string CurrentDatabase => cboDatabase.Text;
        private string CurrentTable => listBox1.SelectedValue as string;

        private void CboDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                Text = $"Current Database: {CurrentDatabase}";

                var no = 0;

                var items = (from it in _entities
                             where it.Database == CurrentDatabase
                             select new BindItem(it.Table, $"[{++no:d3}] {it.Table}")).ToList();

                listBox1.BindTo(items);

                ShowTableSizes();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private BindingSource _bsTableSizes = new BindingSource();

        private void ShowTableSizes()
        {
            var tables = BLL.GetTableStatuses(CurrentDatabase).ToList();

            ulong sumUlong(IEnumerable<ulong> nos) => nos.Aggregate((a, b) => a + b);

            var totals = new TableStatus
            {
                Name = "(total)",
                Rows = sumUlong(tables.Select(i => i.Rows)),
                Data_Length = sumUlong(tables.Select(i => i.Data_Length)),
                Index_Length = sumUlong(tables.Select(i => i.Index_Length)),
                Auto_Increment = 0,
                Collation = null,
                Engine = null
            };

            tables.Add(totals);

            _bsTableSizes.DataSource = tables;
            _bsTableSizes.Sort = "Rows";

            dgvTableSizes.DataSource = _bsTableSizes;
            dgvTableSizes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTableSizes.AutoResizeColumns();
            dgvTableSizes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            foreach (DataGridViewColumn column in dgvTableSizes.Columns)
            {
                if (column.Name.IsMatch("Rows|Length|Increment") || column.ValueType == typeof(ulong))
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    column.DefaultCellStyle.Format = "n0";
                }
            }
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoaded)
                SystemSettings.Set(CurrentDatabase, CurrentTable);

            SelectTable();
        }

        private void SetTabTitles()
        {
            tabPage10.Text = "ET Methods";

            tabPage1.Text = "C# Models";
            codeTextCtrl1.Label = "Database Model";
            codeTextCtrl2.Label = "View Model";

            tabPage2.Text = "C# Mapper";
            codeTextCtrl3.Label = "Database To View";
            codeTextCtrl4.Label = "View To Database";

            tabPage3.Text = "C# Enum & ET";
            codeTextCtrl5.Label = "Enum Types";
            codeTextCtrl6.Label = "ET, Fk, Modeller Classes";

            tabPage4.Text = "Web Controllers";
            codeTextCtrl7.Label = "MVC Actions";
            codeTextCtrl8.Label = "Web API Actions";

            tabPage5.Text = codeTextCtrl9.Label = "DL View";
            tabPage6.Text = codeTextCtrl10.Label = "DL Edit";
            tabPage7.Text = codeTextCtrl11.Label = "DIV View";
            tabPage8.Text = codeTextCtrl12.Label = "DIV Edit";
            tabPage9.Text = "CSS Style";
            
            tabPage11.Text = "Table Definition";
            tabPage12.Text = "Table Sizes";
        }

        private void SetLabelTexts()
        {
            //  title of the labels
            label1.Text  = "Database";
            label2.Text  = "Table";
            label3.Text  = "Data Type";
            label4.Text  = "Position";
            label5.Text  = "Data Model Code";
            label6.Text  = "View Model Code";
            label7.Text  = "View Model Code (FK)";
            label8.Text  = "Data To View";
            label9.Text  = "Data To View (FK)";
            label10.Text = "View To Data";

            foreach (var textBox in panel3.Controls.OfType<TextBox>())
            {
                textBox.ReadOnly = true;
            }
        }

        private void SelectTable()
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                var mgr = TableManager.Get(CurrentDatabase, CurrentTable);

                if (mgr == null) return;

                codeTextCtrl1.Text = mgr.GetDataModelClass();
                codeTextCtrl2.Text = mgr.GetViewModelClass();
                codeTextCtrl3.Text = mgr.GetDataToView();
                codeTextCtrl4.Text = mgr.GetViewToData();

                if (codeTextCtrl5.Tag == null || codeTextCtrl5.Tag.Equals(mgr.Entity.Database) == false)
                {
                    codeTextCtrl5.Text = mgr.GetEnumSetTypes();
                    codeTextCtrl5.Tag = mgr.Entity.Database;
                }

                codeTextCtrl6.Text = DatabaseManager.GetFileContent("Files\\EtMethods.txt");
                codeTextCtrl7.Text = DatabaseManager.GetFileContent("Files\\ActionResults.txt").Replace("{Entity}", mgr.DmClass);
                codeTextCtrl8.Text = DatabaseManager.GetFileContent("Files\\WebApiActions.txt").Replace("{Entity}", mgr.DmClass);

                codeTextCtrl9.Text = mgr.Ui.GetViewOneDL();
                codeTextCtrl10.Text = mgr.Ui.GetEditOneDL();
                codeTextCtrl11.Text = mgr.Ui.GetViewOneDV();
                codeTextCtrl12.Text = mgr.Ui.GetEditOneDV();

                codeTextCtrl13.Text = GetFileContent("Files\\Css.txt");


                var oldFont = dgvDefinition.Font;

                //  table definition and size
                var records = mgr.Records;
                DataGridViewHelper.Columns("Column", "ColumnType", "NetType", "EnumDataType", "MaxLength",
                                           "IsNullable", "IsUnsigned")
                                  .DataSource(records)
                                  .For(dgvDefinition);

                //dgvDefinition.DataSource = mgr.Records;
                dgvDefinition.Font = oldFont;
                dgvDefinition.AutoResizeColumns();
                dgvDefinition.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dgvDefinition.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                panel3.Controls.OfType<TextBox>().ForEach(i => i.DataBindings.Clear());
                panel3.Controls.OfType<CheckBox>().ForEach(i => i.DataBindings.Clear());

                textBox1.DataBindings.Add("Text", records, "Database");
                textBox2.DataBindings.Add("Text", records, "Table");
                textBox3.DataBindings.Add("Text", records, "DataType");
                textBox4.DataBindings.Add("Text", records, "Position");
                textBox5.DataBindings.Add("Text", records, "DataModelCode");
                textBox6.DataBindings.Add("Text", records, "ViewModelCode");
                textBox7.DataBindings.Add("Text", records, "ViewModelFkCode");
                textBox8.DataBindings.Add("Text", records, "DataToView");
                textBox9.DataBindings.Add("Text", records, "DataToViewFk");
                textBox10.DataBindings.Add("Text", records, "ViewToData");

                checkBox1.DataBindings.Add("Checked", records, "IsLocalKey");
                checkBox2.DataBindings.Add("Checked", records, "IsForeignKey");
                //checkBox3.DataBindings.Add("Checked", records, "IsLocalKey");
                //checkBox4.DataBindings.Add("Checked", records, "IsLocalKey");
                //textBox10.DataBindings.Add("Text", records, "ViewToData");

                ReadTabPage(0);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private string GetFileContent(string filename)
        {
            var fi = new FileInfo(filename);

            if (fi.Exists)
                return File.ReadAllText(fi.FullName);

            return string.Empty;
        }
    }
}
