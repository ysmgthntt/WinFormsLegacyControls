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

            // OwnerDraw
            menuItem6.OwnerDraw = true;
            menuItem6.MeasureItem += MenuItem_MeasureItem;
            menuItem6.DrawItem += MenuItem_DrawItem;

            menuItem9.OwnerDraw = true;
            menuItem9.MeasureItem += MenuItem_MeasureItem;
            menuItem9.DrawItem += MenuItem_DrawItem;

            statusBarPanel3.Style = StatusBarPanelStyle.OwnerDraw;
            statusBar1.DrawItem += StatusBar1_DrawItem;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // bug?
            statusBarPanel2.ToolTipText = textBox1.Text;
        }

        private void menuItem5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "menuItem5");
        }

        private void menuItem8_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "menuItem8: " + contextMenu1.SourceControl?.Name);
        }

        private void MenuItem_MeasureItem(object? sender, MeasureItemEventArgs e)
        {
            var menuItem = (MenuItem)sender!;
            var size = TextRenderer.MeasureText(menuItem.Text, SystemInformation.MenuFont);
            e.ItemWidth = (int)size.Width + SystemInformation.MenuCheckSize.Width;
            e.ItemHeight = (int)size.Height;
        }

        private void MenuItem_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var menuItem = (MenuItem)sender!;
            e.DrawBackground();
            var pt = new Point(e.Bounds.X + SystemInformation.MenuCheckSize.Width, e.Bounds.Y);
            TextRenderer.DrawText(e.Graphics, menuItem.Text, e.Font, pt, e.ForeColor);
            e.DrawFocusRectangle();
        }

        private void StatusBar1_DrawItem(object sender, StatusBarDrawItemEventArgs e)
        {
            e.DrawBackground();
            TextRenderer.DrawText(e.Graphics, e.Panel.Text, e.Font, e.Bounds, e.ForeColor);
        }
    }
}
