using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            statusBarPanel2.ToolTipText = textBox1.Text;
        }

        private void menuItem5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "menuItem5");
        }

        private void menuItem8_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "menuItem8");
        }
    }
}
