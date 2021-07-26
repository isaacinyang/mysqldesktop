using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ansyl;

namespace MySqlDesktop
{
    public sealed partial class CodeTextCtrl : UserControl
    {
        public CodeTextCtrl()
        {
            InitializeComponent();

            this.Dock = DockStyle.Fill;
            btnCopy.Click += BtnCopy_Click;
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();

            var str = richTextBox1.Text;
            if (str.IsNullOrWhite() == false)
                Clipboard.SetText(richTextBox1.Text);
        }

        public string Label
        {
            get => label1.Text;
            set => label1.Text = value;
        }

        public override string Text
        {
            get => richTextBox1.Text;
            set => richTextBox1.Text = value;
        }
    }
}
