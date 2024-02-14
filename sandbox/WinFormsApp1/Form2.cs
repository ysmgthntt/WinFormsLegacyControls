using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsLegacyControls;

namespace WinFormsApp1
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();

            menuItem2.Click += (_, _) => Close();
            menuItem3.Click += MenuItem3_Click;
            menuItem4.Click += MenuItem4_Click;

            mnuNewWindow.Click += (_, _) => AddNewMdiForm();
            mnuCascade.Click += (_, _) => LayoutMdi(MdiLayout.Cascade);
            mnuTileVertical.Click += (_, _) => LayoutMdi(MdiLayout.TileVertical);
            mnuTileHorizontal.Click += (_, _) => LayoutMdi(MdiLayout.TileHorizontal);
            mnuCloseAll.Click += (_, _) => {
                foreach (Form form in MdiChildren)
                    form.Close();
            };
            mnuArrangeIcons.Click += (_, _) => LayoutMdi(MdiLayout.ArrangeIcons);

            var command = new SimpleCommand(_ => MessageBox.Show(this, "Container Command"), _ => true);
            menuItem7.Command = command;

            /*
            while (_count < 100)
            {
                _count++;

                Form form = new Form();
                form.MdiParent = this;
                form.Text = "MDI Form " + _count;
                form.Show();
            }
            */
        }

        private int _count = 0;

        private void MenuItem3_Click(object? sender, EventArgs e)
        {
            AddNewMdiForm();
        }

        private void AddNewMdiForm()
        {
            _count++;

            Form form = new Form();
            form.MdiParent = this;
            form.Text = "MDI Form " + _count;

            MainMenu mainMenu = new MainMenu();
            MenuItem file = new MenuItem("&File");
            file.MergeType = MenuMerge.MergeItems;
            MenuItem close = new MenuItem("&Close " + form.Text, (_, _) => form.Close());
            file.MenuItems.Add(close);
            mainMenu.MenuItems.Add(file);
            mainMenu.MenuItems.Add("MDI Menu " + _count);
            mainMenu.SetMenu(form, true);
            form.Click += (s, e) =>
            {
                ((Form?)s)?.SetMenu(null);
            };

            MenuItem commandMenu = new MenuItem("M&DI Command " + _count);
            file.MenuItems.Add(commandMenu);
            var command = new SimpleCommand(_ => MessageBox.Show(this, "MDI Command " + _count), _ => true);
            commandMenu.Command = command;

            form.Show();
        }

        private void MenuItem4_Click(object? sender, EventArgs e)
        {
            IsMdiContainer = !IsMdiContainer;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.GetMenu() is null)
                this.SetMenu(mainMenu1);
            else
                this.SetMenu(null);
        }
    }
}
