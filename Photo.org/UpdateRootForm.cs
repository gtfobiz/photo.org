using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Photo.org
{
    public partial class UpdateRootForm : Form
    {
        private Dictionary<Guid, string> m_Paths = new Dictionary<Guid, string>();

        public UpdateRootForm()
        {
            InitializeComponent();

            PopulateCombo();
        }

        private void PopulateCombo()
        {
            m_Paths = Database.QueryPaths();

            foreach (string path in m_Paths.Values)
                f_OldRoot.Items.Add(path);
        }

        private void cmdBrowseNewPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    f_NewRoot.Text = fbd.SelectedPath;
            }
        }

        private void cmdOk_Click(object sender, EventArgs e)
        {
            Database.UpdateRootFolder(m_Paths, f_OldRoot.Text.ToLower(), f_NewRoot.Text.ToLower());

            MessageBox.Show("done!");
        }
    }
}
