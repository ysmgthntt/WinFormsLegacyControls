﻿namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mainMenu1 = new WinFormsLegacyControls.MainMenu(components);
            menuItem1 = new WinFormsLegacyControls.MenuItem();
            menuItem4 = new WinFormsLegacyControls.MenuItem();
            menuItem5 = new WinFormsLegacyControls.MenuItem();
            menuItem6 = new WinFormsLegacyControls.MenuItem();
            menuItem2 = new WinFormsLegacyControls.MenuItem();
            menuItem3 = new WinFormsLegacyControls.MenuItem();
            contextMenu1 = new WinFormsLegacyControls.ContextMenu();
            menuItem7 = new WinFormsLegacyControls.MenuItem();
            menuItem8 = new WinFormsLegacyControls.MenuItem();
            menuItem9 = new WinFormsLegacyControls.MenuItem();
            toolTip1 = new ToolTip(components);
            toolBar1 = new WinFormsLegacyControls.ToolBar();
            toolBarButton1 = new WinFormsLegacyControls.ToolBarButton();
            toolBarButton2 = new WinFormsLegacyControls.ToolBarButton();
            toolBarButton3 = new WinFormsLegacyControls.ToolBarButton();
            statusBar1 = new WinFormsLegacyControls.StatusBar();
            statusBarPanel1 = new WinFormsLegacyControls.StatusBarPanel();
            statusBarPanel2 = new WinFormsLegacyControls.StatusBarPanel();
            statusBarPanel3 = new WinFormsLegacyControls.StatusBarPanel();
            panel1 = new Panel();
            button1 = new Button();
            textBox1 = new TextBox();
            ((System.ComponentModel.ISupportInitialize)statusBarPanel1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)statusBarPanel2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)statusBarPanel3).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // mainMenu1
            // 
            mainMenu1.MenuItems.AddRange(new WinFormsLegacyControls.MenuItem[] { menuItem1, menuItem2, menuItem3 });
            // 
            // menuItem1
            // 
            menuItem1.Index = 0;
            menuItem1.MenuItems.AddRange(new WinFormsLegacyControls.MenuItem[] { menuItem4, menuItem5, menuItem6 });
            menuItem1.Text = "menuItem1";
            // 
            // menuItem4
            // 
            menuItem4.Index = 0;
            menuItem4.Text = "menuItem4";
            // 
            // menuItem5
            // 
            menuItem5.Index = 1;
            menuItem5.Shortcut = Shortcut.CtrlM;
            menuItem5.Text = "menuItem5";
            // 
            // menuItem6
            // 
            menuItem6.Index = 2;
            menuItem6.Text = "menuItem6";
            // 
            // menuItem2
            // 
            menuItem2.Index = 1;
            menuItem2.Text = "menuItem2";
            // 
            // menuItem3
            // 
            menuItem3.Index = 2;
            menuItem3.Text = "menuItem3";
            // 
            // contextMenu1
            // 
            contextMenu1.MenuItems.AddRange(new WinFormsLegacyControls.MenuItem[] { menuItem7, menuItem8, menuItem9 });
            // 
            // menuItem7
            // 
            menuItem7.Index = 0;
            menuItem7.Text = "menuItem7";
            // 
            // menuItem8
            // 
            menuItem8.Index = 1;
            menuItem8.Shortcut = Shortcut.CtrlShiftC;
            menuItem8.Text = "menuItem8";
            // 
            // menuItem9
            // 
            menuItem9.Index = 2;
            menuItem9.Text = "menuItem9";
            // 
            // toolBar1
            // 
            toolBar1.Buttons.AddRange(new WinFormsLegacyControls.ToolBarButton[] { toolBarButton1, toolBarButton2, toolBarButton3 });
            toolBar1.DropDownArrows = true;
            toolBar1.Location = new Point(0, 0);
            toolBar1.Name = "toolBar1";
            toolBar1.ShowToolTips = true;
            toolBar1.Size = new Size(800, 62);
            toolBar1.TabIndex = 0;
            toolTip1.SetToolTip(toolBar1, "toolbar");
            // 
            // toolBarButton1
            // 
            toolBarButton1.Name = "toolBarButton1";
            toolBarButton1.Text = "toolBarButton1";
            toolBarButton1.ToolTipText = "ta";
            // 
            // toolBarButton2
            // 
            toolBarButton2.Name = "toolBarButton2";
            toolBarButton2.Text = "toolBarButton2";
            toolBarButton2.ToolTipText = "tb";
            // 
            // toolBarButton3
            // 
            toolBarButton3.DropDownMenu = contextMenu1;
            toolBarButton3.Name = "toolBarButton3";
            toolBarButton3.Style = WinFormsLegacyControls.ToolBarButtonStyle.DropDownButton;
            toolBarButton3.Text = "toolBarButton3";
            toolBarButton3.ToolTipText = "tc";
            // 
            // statusBar1
            // 
            statusBar1.Location = new Point(0, 417);
            statusBar1.Name = "statusBar1";
            statusBar1.Panels.AddRange(new WinFormsLegacyControls.StatusBarPanel[] { statusBarPanel1, statusBarPanel2, statusBarPanel3 });
            statusBar1.ShowPanels = true;
            statusBar1.Size = new Size(800, 33);
            statusBar1.TabIndex = 1;
            statusBar1.Text = "statusBar1";
            toolTip1.SetToolTip(statusBar1, "statusbar");
            // 
            // statusBarPanel1
            // 
            statusBarPanel1.Name = "statusBarPanel1";
            statusBarPanel1.Text = "statusBarPanel1";
            statusBarPanel1.ToolTipText = "sa";
            // 
            // statusBarPanel2
            // 
            statusBarPanel2.Name = "statusBarPanel2";
            statusBarPanel2.Text = "statusBarPanel2";
            statusBarPanel2.ToolTipText = "sb";
            // 
            // statusBarPanel3
            // 
            statusBarPanel3.Name = "statusBarPanel3";
            statusBarPanel3.Text = "statusBarPanel3";
            statusBarPanel3.ToolTipText = "sc";
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(textBox1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 62);
            panel1.Name = "panel1";
            panel1.Size = new Size(800, 355);
            panel1.TabIndex = 2;
            // 
            // button1
            // 
            button1.Location = new Point(168, 4);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 1;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 6);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(150, 31);
            textBox1.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            contextMenu1.SetContextMenu(this, true);
            Controls.Add(panel1);
            Controls.Add(statusBar1);
            Controls.Add(toolBar1);
            mainMenu1.SetMenu(this, true);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)statusBarPanel1).EndInit();
            ((System.ComponentModel.ISupportInitialize)statusBarPanel2).EndInit();
            ((System.ComponentModel.ISupportInitialize)statusBarPanel3).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private WinFormsLegacyControls.MainMenu mainMenu1;
        private WinFormsLegacyControls.MenuItem menuItem1;
        private WinFormsLegacyControls.MenuItem menuItem4;
        private WinFormsLegacyControls.MenuItem menuItem5;
        private WinFormsLegacyControls.MenuItem menuItem6;
        private WinFormsLegacyControls.MenuItem menuItem2;
        private WinFormsLegacyControls.MenuItem menuItem3;
        private WinFormsLegacyControls.ContextMenu contextMenu1;
        private WinFormsLegacyControls.MenuItem menuItem7;
        private WinFormsLegacyControls.MenuItem menuItem8;
        private WinFormsLegacyControls.MenuItem menuItem9;
        private ToolTip toolTip1;
        private WinFormsLegacyControls.ToolBar toolBar1;
        private WinFormsLegacyControls.ToolBarButton toolBarButton1;
        private WinFormsLegacyControls.ToolBarButton toolBarButton2;
        private WinFormsLegacyControls.ToolBarButton toolBarButton3;
        private WinFormsLegacyControls.StatusBar statusBar1;
        private WinFormsLegacyControls.StatusBarPanel statusBarPanel1;
        private WinFormsLegacyControls.StatusBarPanel statusBarPanel2;
        private WinFormsLegacyControls.StatusBarPanel statusBarPanel3;
        private Panel panel1;
        private Button button1;
        private TextBox textBox1;
    }
}