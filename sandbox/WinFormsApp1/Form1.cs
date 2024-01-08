using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // No designer support
            menuItem5.Click += (_, _) => MessageBox.Show(this, "menuItem5");
            menuItem8.Click += (_, _) => MessageBox.Show(this, "menuItem8");

            toolBar1.SetToolTip(toolTip1);
            statusBar1.SetToolTip(toolTip1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            statusBarPanel2.ToolTipText = textBox1.Text;
        }
    }
}
