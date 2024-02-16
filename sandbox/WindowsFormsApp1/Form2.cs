using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();

            mnuNewWindow.Click += (_, _) => AddNewMdiForm();
            mnuCascade.Click += (_, _) => LayoutMdi(MdiLayout.Cascade);
            mnuTileVertical.Click += (_, _) => LayoutMdi(MdiLayout.TileVertical);
            mnuTileHorizontal.Click += (_, _) => LayoutMdi(MdiLayout.TileHorizontal);
            mnuCloseAll.Click += (_, _) => {
                foreach (Form form in MdiChildren)
                    form.Close();
            };
            mnuArrangeIcons.Click += (_, _) => LayoutMdi(MdiLayout.ArrangeIcons);

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

        private void menuItem2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private int _count = 0;

        private void menuItem3_Click(object sender, EventArgs e)
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
            mainMenu.MenuItems.Add("MDI Menu " + _count, (_, _) => MessageBox.Show(this, "MDI Menu " + _count));
            form.Menu = mainMenu;
            form.Click += (s, e) =>
            {
                ((Form)s).Menu = null;
            };

            var mm = form.MergedMenu;
            if (mm is not null)
            {
                var m1 = mm.MenuItems[4];
                m1.Click += (_, _) => MessageBox.Show(this, "m1");
                var m2 = mm.MenuItems[5];
                m2.Click += (_, _) => MessageBox.Show(this, "m2");
            }

            form.Show();
        }

        private void menuItem4_Click(object sender, EventArgs e)
        {
            IsMdiContainer = !IsMdiContainer;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Menu is null)
                Menu = mainMenu1;
            else
                Menu = null;
        }
    }
}
