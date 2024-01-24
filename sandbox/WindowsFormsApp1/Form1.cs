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

            MenuItem[] menus = [menuItem1, menuItem2, menuItem3, menuItem4, menuItem5, menuItem6, menuItem7, menuItem8, menuItem9, menuItem10, menuItem11, menuItem12, menuItem13];

            for (int i = 0; i < menus.Length; i++)
            {
                MenuItem menuItem = menus[i];
                menuItem.Name = "menuItem" + (i + 1);
                menuItem.Click += MenuItem_Click;
                menuItem.Select += MenuItem_Select;
                menuItem.Popup += MenuItem_Popup;
            }

            mainMenu1.Collapse += (_, _) => Trace.WriteLine("mainMenu_Collapse");
            contextMenu1.Popup += (_, _) => Trace.WriteLine("contextMenu_Popup");
            contextMenu1.Collapse += (_, _) =>
            {
                Trace.WriteLine("contextMenu_Collapse");
            };

            this.MenuStart += (_, _) => Trace.WriteLine("Form_MenuStart");
            this.MenuComplete += (_, _) => Trace.WriteLine("Form_MenuComplete");

            contextMenu2.Popup += (_, _) => Trace.WriteLine("contextMenu2_Popup");
            contextMenu2.Collapse += (_, _) =>
            {
                Trace.WriteLine("contextMenu2_Collapse");
            };

            //this.ContextMenu = contextMenu1;

            // OwnerDraw
            menuItem6.OwnerDraw = true;
            menuItem6.MeasureItem += MenuItem_MeasureItem;
            menuItem6.DrawItem += MenuItem_DrawItem;

            menuItem9.OwnerDraw = true;
            menuItem9.MeasureItem += MenuItem_MeasureItem;
            menuItem9.DrawItem += MenuItem_DrawItem;

            statusBarPanel2.Style = StatusBarPanelStyle.OwnerDraw;
            statusBar1.DrawItem += StatusBar1_DrawItem;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // bug?
            statusBarPanel2.ToolTipText = textBox1.Text;

            menuItem5.ShowShortcut = !menuItem5.ShowShortcut;

            this.RightToLeftLayout = !this.RightToLeftLayout;
        }

        private void MenuItem_Click(object? sender, EventArgs e)
        {
            var menuItem = (MenuItem)sender!;
            MessageBox.Show(this, menuItem.Name + ": " + menuItem.GetContextMenu()?.SourceControl?.Name);
        }

        private void MenuItem_Select(object? sender, EventArgs e)
        {
            var menuItem = (MenuItem)sender!;
            Debug.WriteLine("Select: " + menuItem.Name);
        }

        private void MenuItem_Popup(object? sender, EventArgs e)
        {
            var menuItem = (MenuItem)sender!;
            Debug.WriteLine("Popup: " + menuItem.Name);
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
            e.Graphics.FillRectangle(Brushes.Red, e.Bounds);
            //e.Graphics.DrawString(e.Panel.Text, e.Font!, SystemBrushes.ControlText, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Panel.Text, e.Font, e.Bounds, e.ForeColor);
        }

        private void toolBar1_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
        {
            if (e.Button == toolBarButton1)
            {
                new Form2().Show();
            }
            else if (e.Button == toolBarButton2)
            {
                /*
                mainMenu1.RightToLeft = mainMenu1.RightToLeft == RightToLeft.Yes ? RightToLeft.No : RightToLeft.Yes;
                contextMenu1.RightToLeft = contextMenu1.RightToLeft == RightToLeft.Yes ? RightToLeft.No : RightToLeft.Yes;
                contextMenu2.RightToLeft = contextMenu2.RightToLeft == RightToLeft.Yes ? RightToLeft.No : RightToLeft.Yes;
                */
                this.RightToLeft = this.RightToLeft == RightToLeft.Yes ? RightToLeft.No : RightToLeft.Yes;
            }
        }

        private void statusBar1_PanelClick(object sender, StatusBarPanelClickEventArgs e)
        {
            Debug.WriteLine("statusBar1_PanelClick: " + e.StatusBarPanel.Name);
        }
    }
}
