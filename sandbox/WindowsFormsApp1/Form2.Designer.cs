namespace WindowsFormsApp1
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
            this.components = new System.ComponentModel.Container();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.mnuNewWindow = new System.Windows.Forms.MenuItem();
            this.mnuCascade = new System.Windows.Forms.MenuItem();
            this.mnuTileVertical = new System.Windows.Forms.MenuItem();
            this.mnuTileHorizontal = new System.Windows.Forms.MenuItem();
            this.mnuCloseAll = new System.Windows.Forms.MenuItem();
            this.mnuArrangeIcons = new System.Windows.Forms.MenuItem();
            this.menuItem7 = new System.Windows.Forms.MenuItem();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem3,
            this.menuItem4,
            this.menuItem5,
            this.menuItem7});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem2});
            this.menuItem1.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.menuItem1.Text = "&File";
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 0;
            this.menuItem2.MergeOrder = 100;
            this.menuItem2.Text = "&Close";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 1;
            this.menuItem3.Text = "&Add MDI Form";
            this.menuItem3.Click += new System.EventHandler(this.menuItem3_Click);
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 2;
            this.menuItem4.Text = "Is&MDIContainer";
            this.menuItem4.Click += new System.EventHandler(this.menuItem4_Click);
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 3;
            this.menuItem5.MdiList = true;
            this.menuItem5.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuNewWindow,
            this.mnuCascade,
            this.mnuTileVertical,
            this.mnuTileHorizontal,
            this.mnuCloseAll,
            this.mnuArrangeIcons});
            this.menuItem5.Text = "&Window";
            // 
            // mnuNewWindow
            // 
            this.mnuNewWindow.Index = 0;
            this.mnuNewWindow.Text = "&New Window";
            // 
            // mnuCascade
            // 
            this.mnuCascade.Index = 1;
            this.mnuCascade.Text = "&Cascade";
            // 
            // mnuTileVertical
            // 
            this.mnuTileVertical.Index = 2;
            this.mnuTileVertical.Shortcut = System.Windows.Forms.Shortcut.CtrlV;
            this.mnuTileVertical.Text = "Tile &Vertical";
            // 
            // mnuTileHorizontal
            // 
            this.mnuTileHorizontal.Index = 3;
            this.mnuTileHorizontal.Shortcut = System.Windows.Forms.Shortcut.CtrlH;
            this.mnuTileHorizontal.Text = "Tile &Horizontal";
            // 
            // mnuCloseAll
            // 
            this.mnuCloseAll.Index = 4;
            this.mnuCloseAll.Text = "C&lose All";
            // 
            // mnuArrangeIcons
            // 
            this.mnuArrangeIcons.Index = 5;
            this.mnuArrangeIcons.Text = "&Arrange Icons";
            // 
            // menuItem7
            // 
            this.menuItem7.Index = 4;
            this.menuItem7.Text = "Container Menu";
            // 
            // contextMenu1
            // 
            this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem6});
            // 
            // menuItem6
            // 
            this.menuItem6.Index = 0;
            this.menuItem6.MdiList = true;
            this.menuItem6.Text = "&Window";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button1.Location = new System.Drawing.Point(676, 404);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 34);
            this.button1.TabIndex = 1;
            this.button1.Text = "MainMenu";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.ContextMenu = this.contextMenu1;
            this.Controls.Add(this.button1);
            this.IsMdiContainer = true;
            this.Menu = this.mainMenu1;
            this.Name = "Form2";
            this.Text = "Form2";
            this.ResumeLayout(false);

        }

        #endregion

        private MainMenu mainMenu1;
        private MenuItem menuItem1;
        private MenuItem menuItem2;
        private MenuItem menuItem3;
        private MenuItem menuItem4;
        private MenuItem menuItem5;
        private ContextMenu contextMenu1;
        private MenuItem menuItem6;
        private Button button1;
        private MenuItem mnuNewWindow;
        private MenuItem mnuCascade;
        private MenuItem mnuTileVertical;
        private MenuItem mnuTileHorizontal;
        private MenuItem mnuCloseAll;
        private MenuItem mnuArrangeIcons;
        private MenuItem menuItem7;
    }
}