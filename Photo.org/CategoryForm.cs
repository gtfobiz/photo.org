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
    public partial class CategoryForm : Form
    {
        internal List<Guid> SelectedCategories = new List<Guid>();

        private List<Category> m_RecentCategories = null;
        private SortedDictionary<string, Category> m_AllCategories = null;
        private List<Guid> m_ListedCategories = new List<Guid>();
        private string m_SearchString = "";
        //private bool m_SearchOnlyBeginning = true;
        private CategoryDialogMode m_CategoryDialogMode = CategoryDialogMode.Default;

        internal CategoryDialogMode CategoryDialogMode
        {
            get { return m_CategoryDialogMode; }
            set { m_CategoryDialogMode = value; }
        }

        internal Dictionary<Guid, Category> AllCategories
        {
            set 
            {
                m_AllCategories = new SortedDictionary<string, Category>();
                foreach (Category category in value.Values)
                    m_AllCategories.Add(category.Name.ToLower() + category.Id.ToString(), category);
            }
        }

        internal List<Category> RecentCategories
        {
            set 
            { 
                m_RecentCategories = value;
                RefreshCategories();
            }
        }        

        private void RefreshCategories()
        {
            lstCategories.BeginUpdate();

            lstCategories.Items.Clear();
            m_ListedCategories.Clear();

            if (m_SearchString == "")
            {
                if (m_CategoryDialogMode == CategoryDialogMode.Default)
                {
                    this.Text = "Recent categories";
                    for (int i = m_RecentCategories.Count - 1; i >= 0; i--)
                    {
                        m_ListedCategories.Add(m_RecentCategories[i].Id);
                        lstCategories.Items.Add(m_RecentCategories[i].Name);
                    }
                }
                else
                {
                    this.Text = "...";
                }
            }
            else
            {
                this.Text = (Settings.CategoryFormSearchFromBegin ? "Categories starting with " + m_SearchString : "Categories containing " + m_SearchString);
                foreach (KeyValuePair<string, Category> pair in m_AllCategories)
                {
                    string name = pair.Value.Name.ToLower();
                    if ((Settings.CategoryFormSearchFromBegin && name.StartsWith(m_SearchString)) || (!Settings.CategoryFormSearchFromBegin && name.Contains(m_SearchString)))
                    {
                        m_ListedCategories.Add(pair.Value.Id);
                        lstCategories.Items.Add(pair.Value.Name);
                    }
                }
            }

            lstCategories.EndUpdate();

            if (lstCategories.Items.Count > 0)
                lstCategories.SelectedIndex = 0;
        }

        public CategoryForm()
        {
            InitializeComponent();
        }

        private void CategoryForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();

            if (e.KeyCode == Keys.Return)
                SetCurrentCategoryAsSelected();
        }

        private void CategoryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_RecentCategories = null;
        }

        private void lstCategories_Click(object sender, EventArgs e)
        {
            if (lstCategories.SelectedIndex >= 0)
                SetCurrentCategoryAsSelected();
        }

        private void SetCurrentCategoryAsSelected()
        {
            try
            {
                int last = lstCategories.SelectedIndex;
                int first = (m_CategoryDialogMode == org.CategoryDialogMode.Default && Common.IsShiftPressed() ? 0 : last);

                for (int i=first; i<=last; i++)
                    SelectedCategories.Add(m_ListedCategories[i]);

                this.Close();
            }
            catch
            {
            }
        }

        private void CategoryForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '*')
            {
                Settings.CategoryFormSearchFromBegin = !Settings.CategoryFormSearchFromBegin;
                Database.SaveParameterText("CATEGORY_SEARCH_BEGIN", (Settings.CategoryFormSearchFromBegin ? "1" : "0"));

                RefreshCategories();
                return;
            }

            if (e.KeyChar == '\r')
                return;

            if (e.KeyChar == ' ' && m_SearchString == "")
                return;

            string searchString = m_SearchString;            

            if (e.KeyChar == '\b')
            {
                if (m_SearchString.Length > 0)
                    m_SearchString = m_SearchString.Substring(0, m_SearchString.Length - 1);
            }
            else
            {
                m_SearchString += e.KeyChar.ToString().ToLower();
            }

            if (m_SearchString != searchString)
            {
                lblStartsWith.Text = m_SearchString.Replace(' ', '_');
                RefreshCategories();
            }

            e.Handled = true;
        }
    }
}
