﻿using System;
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
        private bool _canExecute = true;

        public Form1()
        {
            InitializeComponent();

            // No designer support
            //  Event
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

            statusBarPanel2.Style = StatusBarPanelStyle.OwnerDraw;
            statusBar1.DrawItem += StatusBar1_DrawItem;


            // Command
            var command1 = new SimpleCommand(p => MessageBox.Show(this, $"command1: {p}"), _ => _canExecute);
            menuItem11.Command = command1;
            menuItem11.CommandParameter = nameof(menuItem11);
            menuItem15.Command = command1;
            menuItem15.CommandParameter = nameof(menuItem15);
            menuItem17.Command = command1;
            menuItem17.CommandParameter = nameof(menuItem17);
            toolBarButton3.Command = command1;
            toolBarButton3.CommandParameter = nameof(toolBarButton3);

            var command2 = new SimpleCommand(_ => { _canExecute = !_canExecute; command1.RaiseCanExecuteChanged(); }, _ => true);
            menuItem10.Command = command2;
            menuItem14.Command = command2;
            menuItem16.Command = command2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*
            statusBarPanel2.ToolTipText = textBox1.Text;

            menuItem5.ShowShortcut = !menuItem5.ShowShortcut;

            this.RightToLeftLayout = !this.RightToLeftLayout;
            toolBar1.RightToLeftLayout = !toolBar1.RightToLeftLayout;
            statusBar1.RightToLeftLayout = !statusBar1.RightToLeftLayout;
            */

            /*
            Debug.WriteLine(mainMenu1.ToString());
            Debug.WriteLine(contextMenu1.ToString());
            Debug.WriteLine(menuItem6.ToString());
            Debug.WriteLine(toolBar1.ToString());
            Debug.WriteLine(toolBarButton2.ToString());
            Debug.WriteLine(statusBar1.ToString());
            Debug.WriteLine(statusBarPanel3.ToString());
            */

            toolBarButton2.Text = textBox1.Text;
            toolBarButton2.Pushed = !toolBarButton2.Pushed;

            var clone = menuItem5.CloneMenu();
            clone.Click += (sender, e) => MessageBox.Show(this, "Clone");
            menuItem2.MenuItems.Add(clone);
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
            var bounds = e.Bounds;
            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
                e.Graphics.FillRectangle(Brushes.Red, bounds);
            int menuCheckWidth = SystemInformation.MenuCheckSize.Width;
            bounds = new Rectangle(bounds.X + menuCheckWidth, bounds.Y, bounds.Width - menuCheckWidth, bounds.Height);
            e.Graphics.DrawRectangle(Pens.Blue, bounds);
            //using Brush brush = new SolidBrush(e.ForeColor);
            //e.Graphics.DrawString(menuItem.Text, e.Font!, brush, bounds, new StringFormat() { Alignment = StringAlignment.Near });
            TextRenderer.DrawText(e.Graphics, menuItem.Text, e.Font, bounds, e.ForeColor, TextFormatFlags.Left);
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
