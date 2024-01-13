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
using WinFormsLegacyControls;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // No designer support
            //  Event
            MenuItem[] menus = [menuItem1, menuItem2, menuItem3, menuItem4, menuItem5, menuItem6, menuItem7, menuItem8, menuItem9];

            for (int i = 0; i < menus.Length; i++)
            {
                MenuItem menuItem = menus[i];
                menuItem.Name = "menuItem" + (i + 1);
                menuItem.Click += MenuItem_Click;
                menuItem.Select += MenuItem_Select;
                menuItem.Popup += MenuItem_Popup;
            }

            mainMenu1.Collapse += (_, _) => Debug.WriteLine("mainMenu_Collapse");
            contextMenu1.Popup += (_, _) => Debug.WriteLine("contextMenu_Popup");
            contextMenu1.Collapse += (_, _) => Debug.WriteLine("contextMenu_Collapse");

            this.MenuStart += (_, _) => Debug.WriteLine("Form_MenuStart");
            this.MenuComplete += (_, _) => Debug.WriteLine("Form_MenuComplete");

            //this.SetContextMenu(contextMenu1);

            //  ToolTip association
            toolBar1.SetToolTip(toolTip1);
            statusBar1.SetToolTip(toolTip1);

            //  TreeNode
            TreeNode treeNode1 = new TreeNode("treeNode1");
            TreeNode treeNode2 = new TreeNode("treeNode2");
            treeNode2.SetContextMenu(contextMenu1);
            TreeNode treeNode3 = new TreeNode("treeNode3");
            treeView1.Nodes.AddRange([treeNode1, treeNode2, treeNode3]);
            treeView1.SetContextMenuForTreeNodesEnabled(true);


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
            statusBarPanel2.ToolTipText = textBox1.Text;
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
            TextRenderer.DrawText(e.Graphics, e.Panel.Text, e.Font, e.Bounds, e.ForeColor);
        }
    }
}
