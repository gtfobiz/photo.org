using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Data;

namespace Photo.org
{
    internal static class Categories
    {
#region public interface

        internal delegate void CategoryAssignmentChangedDelegate(Photo photo, Category category, bool added);
        internal static event CategoryAssignmentChangedDelegate CategoryAssignmentChanged;

        internal static Guid LastCategoryId = Guid.Empty;
        
        internal static void InsertTestCategories()
        {
            m_TreeView.ClearNodes();
            m_UnassignedNode = m_TreeView.Nodes.Add(Guids.Unassigned.ToString(), "");
            m_AllFilesNode = m_TreeView.Nodes.Add(Guids.AllFiles.ToString(), "");

            Guid level1 = AddCategory("Location", Guid.Empty);
            Guid level2 = AddCategory("Finland", level1);
            Guid level3 = AddCategory("Imatra", level2);
            AddCategory("Bronx", level3);
            AddCategory("Imatrankoski", level3);
            level1 = AddCategory("Year", Guid.Empty);
            AddCategory("2009", level1);
            AddCategory("2010", level1);
            AddCategory("2011", level1);
            AddCategory("2012", level1);
            m_TreeView.ClearNodes();
            m_Categories.Clear();
        }

        internal static void AddPhotoCategory(Photo photo, Guid categoryId)
        {
            LastCategoryId = categoryId;

            Database.InsertPhotoCategory(photo.Id, categoryId);
            Database.QueryPhotoCategories(photo);
           
            CategoryAssignmentChanged(photo, m_Categories[categoryId], false);
        }

        internal static void RemovePhotoCategory(Photo photo, Guid categoryId)
        {
            LastCategoryId = categoryId;

            Database.DeletePhotoCategory(photo.Id, categoryId);
            Database.ApplyAutoCategories(photo.Id, Guid.Empty);
            Database.QueryPhotoCategories(photo);

            CategoryAssignmentChanged(photo, m_Categories[categoryId], true);
        }

        internal static Category GetCategoryByGuid(Guid categoryId)
        {
            if (m_Categories.ContainsKey(categoryId))
                return m_Categories[categoryId];

            return null;
        }

        internal static void ShowCategoryDialog(List<Photo> photos)
        {
            using (CategoryForm f = new CategoryForm())
            {
                f.AllCategories = m_Categories;
                f.RecentCategories = m_RecentCategories;

                f.ShowDialog();

                foreach (Guid categoryId in f.SelectedCategories)
                    SetPhotoCategory(photos, categoryId);
            }
        }

        internal static void SetPhotoCategory(List<Photo> photos, Guid categoryId)
        {
            SetPhotoCategory(photos, categoryId, false);
        }

        internal static void SetPhotoCategory(List<Photo> photos, Guid categoryId, bool remove)
        {
            Status.ShowProgress(0, photos.Count, 0);
            Database.BeginTransaction();

            int i = 0;
            foreach (Photo photo in photos)
            {
                if (remove)
                {
                    RemovePhotoCategory(photo, categoryId);
                }
                else if (!Database.CheckPhotoCategory(photo.Id, categoryId))
                {
                    AddPhotoCategory(photo, categoryId);
                    AddToRecentCategories(m_Categories[categoryId]);
                }

                Status.SetProgress(++i);
            }

            Database.Commit();            

            Status.HideProgress();
        }

        internal static void ClearCategories()
        {
            m_TreeView.ClearNodes();
            m_Categories = new Dictionary<Guid, Category>();
            m_RecentCategories = new List<Category>();
        }

        internal static void RemoveSelections()
        {
            m_TreeView.ClearSelections();
        }

#endregion

#region private members

        private static CustomTreeView m_TreeView = null;
        private static Dictionary<Guid, Category> m_Categories = new Dictionary<Guid, Category>();
        private static TreeNode m_AllFilesNode = null;
        private static TreeNode m_UnassignedNode = null;
        private static TreeNode m_MenuTargetNode = null;
        private static List<TreeNode> m_NodesToHideFromResults = new List<TreeNode>();
        private static List<Category> m_RecentCategories = new List<Category>();

#endregion     

#region private methods

        private static void FetchPhotos()
        {
            foreach (TreeNode tn in m_NodesToHideFromResults)
                tn.BackColor = Color.White;
            m_NodesToHideFromResults.Clear();

            List<Guid> required = new List<Guid>();

            foreach (TreeNode tn in m_TreeView.SelectedNodes)
                if (new Guid(tn.Name) != Guids.AllFiles)
                    required.Add(new Guid(tn.Name));

            Thumbnails.FetchPhotos(required);
        }

        private static Guid AddCategory(string name, Guid parent)
        {
            TreeNode parentNode = null;

            if (parent == Guid.Empty || parent == Guids.AllFiles)
            {
                parent = Guid.Empty;
                parentNode = m_AllFilesNode;
            }
            else
            {
                TreeNode[] tna = m_TreeView.Nodes.Find(parent.ToString(), true);
                if (tna.Length == 0)
                    return Guid.Empty;
                parentNode = tna[0];
            }

            Guid id = Guid.NewGuid();
            Category category = new Category(id, parent, name, 0, 0);
            m_Categories.Add(id, category);

            Database.BeginTransaction();

            Database.InsertCategory(id, parent, name);
            Database.InsertCategoryPath(id, id);

            parentNode.Nodes.Add(id.ToString(), name).Tag = category;

            while (parentNode.Parent != null)
            {
                Database.InsertCategoryPath(new Guid(parentNode.Name), id);
                parentNode = parentNode.Parent;
            }

            Database.Commit();

            return id;
        }

        private static void AddToTreeView(Category category)
        {
            TreeNode newNode = null;

            if (category.ParentId == Guid.Empty)
            {
                newNode = m_AllFilesNode.Nodes.Add(category.Id.ToString(), category.Label);
            }
            else
            {
                TreeNode[] tna = m_TreeView.Nodes.Find(category.ParentId.ToString(), true);
                if (tna.Length == 0)
                    return;
                newNode = tna[0].Nodes.Add(category.Id.ToString(), category.Label);
            }
            newNode.Tag = category;

            foreach (Category c in m_Categories.Values)
                if (c.ParentId == category.Id)
                    AddToTreeView(c);
        }

        private static void AddToRecentCategories(Category category)
        {
            if (m_RecentCategories.Contains(category))
                m_RecentCategories.Remove(category);
            m_RecentCategories.Add(category);

            while (m_RecentCategories.Count > 10)
                m_RecentCategories.RemoveAt(0);
        }

        private static void OnHideNodeFromResultsRecurse(ref List<Category> categories, TreeNode treeNode)
        {
            m_NodesToHideFromResults.Add(treeNode);
            treeNode.BackColor = Color.LightPink;

            categories.Add(treeNode.Tag as Category);
            foreach (TreeNode tn in treeNode.Nodes)
                OnHideNodeFromResultsRecurse(ref categories, tn);
        }

        private static List<Category> OnHideNodeFromResults(TreeNode treeNode)
        {
            List<Category> categories = new List<Category>();
            OnHideNodeFromResultsRecurse(ref categories, treeNode);
            return categories;
        }

#endregion

#region initialization

        /// <summary>
        /// Creates a treeview control for the thumbnails
        /// </summary>
        /// <param name="splitContainer">Splitcontainer to contain the treeview control</param>
        internal static void Initialize(Control.ControlCollection controlCollection)
        {
            m_TreeView = new CustomTreeView();
            m_TreeView.Dock = DockStyle.Fill;
            m_TreeView.Font = new Font("Tahoma", 12);
            m_TreeView.ShowRootLines = false;
            m_TreeView.AllowDrop = true;
            m_TreeView.LabelEdit = false;

            m_TreeView.MouseDown += new MouseEventHandler(m_TreeView_MouseDown);
            m_TreeView.MouseMove += new MouseEventHandler(m_TreeView_MouseMove);
            m_TreeView.NodeSelectionChanged += new CustomTreeView.NodeSelectionChangedDelegate(m_TreeView_NodeSelectionChanged);
            m_TreeView.DragEnter += new DragEventHandler(m_TreeView_DragEnter);
            m_TreeView.DragDrop += new DragEventHandler(m_TreeView_DragDrop);
            m_TreeView.AfterLabelEdit += new NodeLabelEditEventHandler(m_TreeView_AfterLabelEdit);
            m_TreeView.BeforeCollapse += new TreeViewCancelEventHandler(m_TreeView_BeforeCollapse);
            m_TreeView.MouseWheelOutsideControl += new CustomTreeView.MouseWheelOutsideControlDelegate(m_TreeView_MouseWheelOutsideControl);

            controlCollection.Add(m_TreeView);
        }

        static void m_TreeView_MouseWheelOutsideControl(object sender, short direction)
        {
            Common.MouseWheelOutsideTreeView(direction > 0);
        }        

        internal static void Populate()
        {
            if (!Database.HasConnection)
                return;

            foreach (DataRow dr in Database.QueryCategories().Rows)
            {
                m_Categories.Add((Guid)dr["CATEGORY_ID"], new Category((Guid)dr["CATEGORY_ID"], (Guid)dr["PARENT_ID"], dr["NAME"].ToString(), (long)dr["PHOTO_COUNT"], dr["COLOR"]));
            }

            m_UnassignedNode = m_TreeView.Nodes.Add(Guids.Unassigned.ToString(), Multilingual.GetText("categories", "unassigned", "Unassigned"));
            //m_RestrictedNode = m_TreeView.Nodes.Add(Guids.Restricted.ToString(), Multilingual.GetText("categories", "restricted", "Restricted")); // ei toimi
            m_AllFilesNode = m_TreeView.Nodes.Add(Guids.AllFiles.ToString(), Multilingual.GetText("categories", "allFiles", "All files"));            

            foreach (Category category in m_Categories.Values)
                if (category.ParentId == Guid.Empty && category.Id != Guids.AllFiles && category.Id != Guids.Unassigned)
                    AddToTreeView(category);

            m_AllFilesNode.Expand();
        }

#endregion

#region events

        static void m_TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (Status.ReadOnly)
                return;

            if (Common.IsShiftPressed() && e.Button == MouseButtons.Left)
            {
                TreeNode tn = m_TreeView.GetNodeAt(e.Location);
                m_TreeView.DoDragDrop(tn, DragDropEffects.Link);
            }
        }

        static void m_TreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (Status.Busy)
                return;

            Point point = new Point(e.X, e.Y);
            TreeNode tn = m_TreeView.GetNodeAt(point);

            if (e.Button == MouseButtons.Right)
                ShowContextMenu(point, tn);
        }

        static void m_TreeView_DragDrop(object sender, DragEventArgs e)
        {
            if (Status.ReadOnly)
                return;

            if (Status.Busy)
                return;

            Point point = new Point(e.X, e.Y);
            point = m_TreeView.PointToClient(point);
            TreeNode tn = m_TreeView.GetNodeAt(point);

            if (tn == null)
                return;

            Guid categoryId = new Guid(tn.Name);            

            if (e.Data.GetDataPresent(typeof(List<Photo>)))
            {
                if (categoryId == Guids.AllFiles || categoryId == Guids.Unassigned)
                    return;

                List<Photo> photos = (List<Photo>)e.Data.GetData(typeof(List<Photo>));

                SetPhotoCategory(photos, categoryId, ((e.KeyState & KeyStates.Shift) == KeyStates.Shift));                
            }
            else if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                if (categoryId == Guids.Unassigned)
                    return;                

                TreeNode movingNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                Category movingCategory = (Category)movingNode.Tag;

                if (movingNode == tn || movingNode.Parent == tn)
                    return;

                if (MessageBox.Show("Do you want to move category " + movingCategory.Name + " under category " + tn.Text + "?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;

                Database.BeginTransaction();

                RemoveCategoryPathFromBranch(movingNode);

                m_TreeView.Nodes.Remove(movingNode);
                tn.Nodes.Add(movingNode);

                CreateCategoryPathForBrach(movingNode);

                if (categoryId == Guids.AllFiles)
                    categoryId = Guid.Empty;

                Database.UpdateCategory(movingCategory.Id, categoryId, movingCategory.Name);

                Database.Commit();
            }
        }

        static void m_TreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        static void m_TreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == m_AllFilesNode)
                e.Cancel = true;
        }

        static void m_TreeView_NodeSelectionChanged(object sender, EventArgs e)
        {
            if (Status.Busy)
                Thumbnails.ClearPhotos();
            else
                FetchPhotos();
        }

        static void m_TreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label == null)
            {
                e.CancelEdit = true;
                return;
            }

            Status.Busy = true;

            Category category = m_Categories[new Guid(e.Node.Name)];
            category.Name = (e.Label);
            Database.UpdateCategory(category.Id, category.ParentId, category.Name);
            m_TreeView.LabelEdit = false;

            Status.Busy = false;
            Status.LabelEdit = false;
        }

#endregion        

        private static void RemoveCategoryPathFromBranch(TreeNode node)
        {
            Database.DeleteCategoryPathByCategory(new Guid(node.Name));

            foreach (TreeNode tn in node.Nodes)
                RemoveCategoryPathFromBranch(tn);
        }

        private static void CreateCategoryPathForBrach(TreeNode node)
        {            
            foreach (TreeNode tn in node.Nodes)
                CreateCategoryPathForBrach(tn);

            Guid categoryId = new Guid(node.Name);

            Database.InsertCategoryPath(categoryId, categoryId);
            string temp = node.Text;

            while (node.Parent != null)
            {
                node = node.Parent;
                Database.InsertCategoryPath(new Guid(node.Name), categoryId);
            }
        }

#region context menus

        private static void ShowContextMenu(Point location, TreeNode targetNode)
        {
            if (targetNode == null)
                return;

            ContextMenu menu = new ContextMenu();
            MenuItem mi = null;

            bool properTargetNode = (targetNode != null && targetNode != m_AllFilesNode && targetNode != m_UnassignedNode);
            bool needSeparator = false;

            if (m_TreeView.SelectedNodes.Count != 0 && properTargetNode && !m_TreeView.SelectedNodes.Contains(targetNode))
            {
                mi = new MenuItem();
                mi.Text = "Hide from results";
                mi.Name = "HideFromResults";
                mi.Click += new EventHandler(contextMenuItem_Click);
                menu.MenuItems.Add(mi);

                needSeparator = true;
            }

            if (targetNode.Nodes.Count != 0)
            {
                mi = new MenuItem();
                mi.Text = "Hide children from results";
                mi.Name = "HideChildrenFromResults";
                mi.Click += new EventHandler(contextMenuItem_Click);
                menu.MenuItems.Add(mi);

                needSeparator = true;
            }

            if (!Status.ReadOnly)
            {
                if (needSeparator)
                    menu.MenuItems.Add("-");

                mi = new MenuItem();
                mi.Text = "New category";
                mi.Name = "NewCategory";
                mi.Click += new EventHandler(contextMenuItem_Click);
                menu.MenuItems.Add(mi);

                if (properTargetNode)
                {
                    mi = new MenuItem();
                    mi.Text = "New category as child";
                    mi.Name = "NewChildCategory";
                    mi.Click += new EventHandler(contextMenuItem_Click);
                    menu.MenuItems.Add(mi);

                    menu.MenuItems.Add("-");

                    mi = new MenuItem();
                    mi.Text = "Rename category";
                    mi.Name = "RenameCategory";
                    mi.Click += new EventHandler(contextMenuItem_Click);
                    menu.MenuItems.Add(mi);

                    mi = new MenuItem();
                    mi.Text = "Delete category";
                    mi.Name = "DeleteCategory";
                    mi.Click += new EventHandler(contextMenuItem_Click);
                    menu.MenuItems.Add(mi);

                    if (m_TreeView.SelectedNodes.Count == 1 && !m_TreeView.SelectedNodes.Contains(targetNode))
                    {
                        menu.MenuItems.Add("-");

                        mi = new MenuItem();
                        mi.Text = "Add auto category";
                        mi.Name = "AddAutoCategory";
                        mi.Click += new EventHandler(contextMenuItem_Click);
                        menu.MenuItems.Add(mi);
                    }

                    menu.MenuItems.Add("-");

                    mi = new MenuItem();
                    mi.Text = "Set category color";
                    mi.Name = "SetCategoryColor";
                    mi.Click += new EventHandler(contextMenuItem_Click);
                    menu.MenuItems.Add(mi);
                }
            }

            m_MenuTargetNode = targetNode;
            menu.Show(m_TreeView, location);
        }

        private static void EditCategoryLabel(TreeNode treeNode)
        {
            treeNode.Text = (treeNode.Tag as Category).Name;
            m_TreeView.LabelEdit = true;
            Status.LabelEdit = true;
            treeNode.BeginEdit();
        }

        static void contextMenuItem_Click(object sender, EventArgs e)
        {
            Guid newNodeGuid = Guid.Empty;

            switch ((sender as MenuItem).Name)
            { 
                case "HideFromResults":
                    Status.Busy = true;
                    Thumbnails.HideCategoriesFromResults(OnHideNodeFromResults(m_MenuTargetNode), Guid.Empty);
                    Status.Busy = false;
                    break;
                case "HideChildrenFromResults":
                    Status.Busy = true;
                    Color color = m_MenuTargetNode.BackColor;
                    List<Category> categories = OnHideNodeFromResults(m_MenuTargetNode);
                    categories.Remove((Category)m_MenuTargetNode.Tag);
                    m_MenuTargetNode.BackColor = color;
                    Thumbnails.HideCategoriesFromResults(categories, (m_TreeView.SelectedNodes.Contains(m_MenuTargetNode) ? (m_MenuTargetNode.Tag as Category).Id : Guid.Empty));
                    Status.Busy = false;
                    break;
                case "NewCategory":
                    if (m_MenuTargetNode == null || m_MenuTargetNode.Parent == null)
                        newNodeGuid = AddCategory("New Category", Guid.Empty);
                    else
                        newNodeGuid = AddCategory("New Category", new Guid(m_MenuTargetNode.Parent.Name));                        
                    break;
                case "NewChildCategory":
                    newNodeGuid = AddCategory("New Category", new Guid(m_MenuTargetNode.Name));                    
                    m_MenuTargetNode.Expand();
                    break;
                case "RenameCategory":
                    EditCategoryLabel(m_MenuTargetNode);
                    break;
                case "DeleteCategory":
                    DeleteCategoryByNodeExt(m_MenuTargetNode);
                    Thumbnails.ClearPhotos();
                    break;
                case "AddAutoCategory":
                    AddAutoCategory(m_MenuTargetNode.Tag as Category);
                    break;
                case "SetCategoryColor":
                    SetCategoryColor(m_MenuTargetNode);                    
                    break;
            }

            if (newNodeGuid != Guid.Empty)
                EditCategoryLabel(m_TreeView.Nodes.Find(newNodeGuid.ToString(), true)[0]);
        }

        private static void SetCategoryColor(TreeNode tn)
        {
            using (ColorDialog cd = new ColorDialog())
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    //if (tn.Nodes.Count > 0)
                        // update child nodes?

                    UpdateCategoryColor(tn, cd.Color, true);
                }
                else
                {
                    // clear color?
                }
        }

        private static void UpdateCategoryColor(TreeNode tn, Color color, bool updateChildren)
        {
            (tn.Tag as Category).Color = color;
            Database.UpdateCategory(tn.Tag as Category);

            if (updateChildren)
                foreach (TreeNode node in tn.Nodes)
                    UpdateCategoryColor(node, color, true);
        }

        private static void AddAutoCategory(Category category)
        {
            if (m_TreeView.SelectedNodes.Count != 1)
                return;

            Category source = (Category)m_TreeView.SelectedNodes[0].Tag;

            if (MessageBox.Show("Add " + category.Name + " as auto category for " + source.Name + "?", "Add auto category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Database.InsertAutoCategory(source.Id, category.Id);
            Database.ApplyAutoCategories(source.Id);
        }

        private static void DeleteCategoryByNode(TreeNode treeNode)
        {
            foreach (TreeNode tn in treeNode.Nodes)
                DeleteCategoryByNode(tn);

            Category category = (Category)treeNode.Tag;
            Database.DeleteCategory(category.Id);
            m_Categories.Remove(category.Id);
        }

        //TODO: tyhmä nimi
        private static void DeleteCategoryByNodeExt(TreeNode treeNode)
        {
            if (MessageBox.Show("Delete category " + treeNode.Text + "?", "Delete category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            Status.ShowProgress();
            Database.BeginTransaction();

            DeleteCategoryByNode(treeNode);
            m_TreeView.RemoveNode(treeNode);

            Database.Commit();
            Status.HideProgress();
        }

#endregion                
    
        internal static void TakeFocus()
        {
            m_TreeView.Focus();
        }
    }

    class Category
    {
        public Guid Id = Guid.Empty;
        public Guid ParentId = Guid.Empty;
        public string Name = "";
        public long PhotoCount = 0;
        public Color Color = Color.Black;
        //private int m_PhotoCount = 0;

        //public int PhotoCount
        //{
        //    get { return m_PhotoCount; }
        //    set { m_PhotoCount = value; }
        //}

        public string Label
        {
            get { return Name; }
            //get { return Name + " (" + PhotoCount.ToString() + ")"; }
        }

        public Category()
        {
        }

        public Category(Guid id, Guid parentId, string name, long photoCount, object color)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            PhotoCount = photoCount;            
            long colorValue = (color == DBNull.Value ? 0 : (long)color);            
            Color = Color.FromArgb((int)colorValue);
        }
    }
}
