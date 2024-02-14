namespace WinFormsApp1
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mainMenu1 = new WinFormsLegacyControls.MainMenu(components);
            menuItem1 = new WinFormsLegacyControls.MenuItem();
            menuItem2 = new WinFormsLegacyControls.MenuItem();
            menuItem7 = new WinFormsLegacyControls.MenuItem();
            menuItem3 = new WinFormsLegacyControls.MenuItem();
            menuItem4 = new WinFormsLegacyControls.MenuItem();
            menuItem5 = new WinFormsLegacyControls.MenuItem();
            mnuNewWindow = new WinFormsLegacyControls.MenuItem();
            mnuCascade = new WinFormsLegacyControls.MenuItem();
            mnuTileVertical = new WinFormsLegacyControls.MenuItem();
            mnuTileHorizontal = new WinFormsLegacyControls.MenuItem();
            mnuCloseAll = new WinFormsLegacyControls.MenuItem();
            mnuArrangeIcons = new WinFormsLegacyControls.MenuItem();
            contextMenu1 = new WinFormsLegacyControls.ContextMenu();
            menuItem6 = new WinFormsLegacyControls.MenuItem();
            button1 = new Button();
            SuspendLayout();
            // 
            // mainMenu1
            // 
            mainMenu1.MenuItems.AddRange(new WinFormsLegacyControls.MenuItem[] { menuItem1, menuItem3, menuItem4, menuItem5 });
            // 
            // menuItem1
            // 
            menuItem1.Index = 0;
            menuItem1.MenuItems.AddRange(new WinFormsLegacyControls.MenuItem[] { menuItem2, menuItem7 });
            menuItem1.MergeType = WinFormsLegacyControls.MenuMerge.MergeItems;
            menuItem1.Text = "&File";
            // 
            // menuItem2
            // 
            menuItem2.Index = 0;
            menuItem2.MergeOrder = 100;
            menuItem2.Text = "&Close";
            // 
            // menuItem7
            // 
            menuItem7.Index = 1;
            menuItem7.Text = "C&ontainer Command";
            // 
            // menuItem3
            // 
            menuItem3.Index = 1;
            menuItem3.Text = "&Add MDI Form";
            // 
            // menuItem4
            // 
            menuItem4.Index = 2;
            menuItem4.Text = "Is&MDIContainer";
            // 
            // menuItem5
            // 
            menuItem5.Index = 3;
            menuItem5.MdiList = true;
            menuItem5.MenuItems.AddRange(new WinFormsLegacyControls.MenuItem[] { mnuNewWindow, mnuCascade, mnuTileVertical, mnuTileHorizontal, mnuCloseAll, mnuArrangeIcons });
            menuItem5.Text = "&Window";
            // 
            // mnuNewWindow
            // 
            mnuNewWindow.Index = 0;
            mnuNewWindow.Text = "&New Window";
            // 
            // mnuCascade
            // 
            mnuCascade.Index = 1;
            mnuCascade.Text = "&Cascade";
            // 
            // mnuTileVertical
            // 
            mnuTileVertical.Index = 2;
            mnuTileVertical.Shortcut = Shortcut.CtrlV;
            mnuTileVertical.Text = "Tile &Vertical";
            // 
            // mnuTileHorizontal
            // 
            mnuTileHorizontal.Index = 3;
            mnuTileHorizontal.Shortcut = Shortcut.CtrlH;
            mnuTileHorizontal.Text = "Tile &Horizontal";
            // 
            // mnuCloseAll
            // 
            mnuCloseAll.Index = 4;
            mnuCloseAll.Text = "C&lose All";
            // 
            // mnuArrangeIcons
            // 
            mnuArrangeIcons.Index = 5;
            mnuArrangeIcons.Text = "&Arrange Icons";
            // 
            // contextMenu1
            // 
            contextMenu1.MenuItems.AddRange(new WinFormsLegacyControls.MenuItem[] { menuItem6 });
            // 
            // menuItem6
            // 
            menuItem6.Index = 0;
            menuItem6.MdiList = true;
            menuItem6.Text = "&Window";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button1.FlatStyle = FlatStyle.System;
            button1.Location = new Point(676, 404);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 1;
            button1.Text = "MainMenu";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            contextMenu1.SetContextMenu(this, true);
            Controls.Add(button1);
            IsMdiContainer = true;
            mainMenu1.SetMenu(this, true);
            Name = "Form2";
            Text = "Form2";
            ResumeLayout(false);
        }

        #endregion

        private WinFormsLegacyControls.MainMenu mainMenu1;
        private WinFormsLegacyControls.MenuItem menuItem1;
        private WinFormsLegacyControls.MenuItem menuItem2;
        private WinFormsLegacyControls.MenuItem menuItem3;
        private WinFormsLegacyControls.MenuItem menuItem4;
        private WinFormsLegacyControls.MenuItem menuItem5;
        private WinFormsLegacyControls.ContextMenu contextMenu1;
        private WinFormsLegacyControls.MenuItem menuItem6;
        private Button button1;
        private WinFormsLegacyControls.MenuItem mnuNewWindow;
        private WinFormsLegacyControls.MenuItem mnuCascade;
        private WinFormsLegacyControls.MenuItem mnuTileVertical;
        private WinFormsLegacyControls.MenuItem mnuTileHorizontal;
        private WinFormsLegacyControls.MenuItem mnuCloseAll;
        private WinFormsLegacyControls.MenuItem mnuArrangeIcons;
        private WinFormsLegacyControls.MenuItem menuItem7;
    }
}
