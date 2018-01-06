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

        internal static void ShowCategoryDialog(CategoryDialogMode mode)
        {
            // jos kuva auki ja tehdään hide, ylä- ja alapalkki jää näkyviin
            // jos ollaan renameamassa categorya, n ja p menee thumbnailseille

            using (CategoryForm f = new CategoryForm())
            {
                switch (mode)
                {
                    case CategoryDialogMode.Select:
                        f.Text = "Select category";
                        break;
                    case CategoryDialogMode.Require:
                        f.Text = "Require category";
                        break;
                    case CategoryDialogMode.Hide:
                        f.Text = "Hide category";
                        break;
                    case CategoryDialogMode.Omit:
                        f.Text = "Omit category";
                        break;
                }

                f.AllCategories = m_Categories;
                f.CategoryDialogMode = mode;                

                f.ShowDialog();

                if (f.SelectedCategories.Count == 0)
                    return;

                switch (mode)
                {
                    case CategoryDialogMode.Select:
                        m_MyTreeView.ClearSelections();
                        m_MyTreeView.SelectNode(m_MyTreeView.FindNode(f.SelectedCategories[0]));
                        FetchPhotos();
                        break;
                    case CategoryDialogMode.Require:
                        m_MyTreeView.SelectNode(m_MyTreeView.FindNode(f.SelectedCategories[0]));
                        FetchPhotos();
                        break;
                    case CategoryDialogMode.Hide:
                        Status.Busy = true;
                        Thumbnails.HideCategoriesFromResults(OnHideNodeFromResults(m_MyTreeView.FindNode(f.SelectedCategories[0])), Guid.Empty);
                        Status.Busy = false;
                        break;
                    case CategoryDialogMode.Locate:
                        Categories.LocateCategory(f.SelectedCategories[0]);
                        break;
                    case CategoryDialogMode.Omit:
                        m_MyTreeView.ClearSelections();
                        List<Guid> required = new List<Guid>();
                        required.Add(f.SelectedCategories[0]);
                        Thumbnails.FetchPhotos(PhotoSearchMode.OmitCategories, required, null);
                        break;
                }

                m_MyTreeView.RefreshOrWhatever();                    
            }
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
            Database.BeginTransaction(); // I doubt this works as is

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
            m_MyTreeView.ClearNodes();        

            m_Categories = new Dictionary<Guid, Category>();
            m_RecentCategories = new List<Category>();
        }

        internal static void RemoveSelections()
        {            
            m_MyTreeView.ClearSelections();
            m_MyTreeView.RefreshOrWhatever();            
        }

#endregion

#region private members

        private static MyTreeView m_MyTreeView = null;
        private static MyTreeNode m_MyAllFilesNode = null;
        private static MyTreeNode m_MyUnassignedNode = null;

        private static Dictionary<Guid, Category> m_Categories = new Dictionary<Guid, Category>();
        private static MyTreeNode m_MyMenuTargetNode = null;
        private static List<TreeNode> m_NodesToHideFromResults = new List<TreeNode>();
        private static List<MyTreeNode> m_MyNodesToHideFromResults = new List<MyTreeNode>();
        private static List<Category> m_RecentCategories = new List<Category>();

#endregion     

#region private methods

        private static void FetchPhotos()
        {
            foreach (MyTreeNode tn in m_MyNodesToHideFromResults)
                if (tn.BackColor != Color.LightGreen)
                    tn.BackColor = Color.White;
            m_MyNodesToHideFromResults.Clear();
            m_MyTreeView.RefreshOrWhatever();

            List<Guid> required = new List<Guid>();

            foreach (MyTreeNode tn in m_MyTreeView.SelectedNodes)
                if ((Guid)tn.Key != Guids.AllFiles)
                    required.Add((Guid)tn.Key);

            Thumbnails.FetchPhotos(PhotoSearchMode.Categories, required, null);
        }

        public static void RebuildCategoryPaths()
        {
            Database.BeginTransaction();

            Database.DeleteAllCategoryPaths();

            foreach (MyTreeNode node in m_MyTreeView.AllNodes.Values)
                if (node.Category != null)
                {
                    Database.InsertCategoryPath(node.Category.Id, node.Category.Id);
                    MyTreeNode parentNode = node.Parent;
                    while (parentNode.Category != null)
                    {
                        Database.InsertCategoryPath(parentNode.Category.Id, node.Category.Id);
                        parentNode = parentNode.Parent;
                    }
                }

            Database.Commit();
        }

        internal static void AddCategory(Guid id, Guid parent, string name, long color)
        {
            MyTreeNode parentNode = null;
            if (parent == Guid.Empty)
            {
                parentNode = m_MyAllFilesNode;
            }
            else
            {
                parentNode = m_MyTreeView.FindNode(parent);
            }

            Category category = new Category(id, parent, name, color);
            m_Categories.Add(id, category);

            Database.InsertCategory(id, parent, name, category.Color);
            Database.InsertCategoryPath(id, id);

            parentNode.Nodes.Add(id, name).Category = category;

            while (parentNode.Category != null)
            {
                Database.InsertCategoryPath(parentNode.Category.Id, id);
                parentNode = parentNode.Parent;
            }
        }

        // implement changes to internal version also!!!
        private static Guid AddCategory(string name, Guid parent)
        {
            MyTreeNode parentNode = null;
            object parentColor = DBNull.Value;

            if (parent == Guid.Empty || parent == Guids.AllFiles)
            {
                parent = Guid.Empty;
                parentNode = m_MyAllFilesNode;
            }
            else
            {
                parentNode = m_MyTreeView.FindNode(parent);
                parentColor = (long)parentNode.Category.Color.ToArgb();                    
            }

            Guid id = Guid.NewGuid();
            Category category = new Category(id, parent, name, parentColor);
            m_Categories.Add(id, category);

            Database.BeginTransaction();

            Database.InsertCategory(id, parent, name, category.Color);
            Database.InsertCategoryPath(id, id);

            parentNode.Nodes.Add(id, name).Category = category;

            while (parentNode.Category != null)
            {
                Database.InsertCategoryPath(parentNode.Category.Id, id);
                parentNode = parentNode.Parent;
            }

            Database.Commit();

            return id;          
        }

        private static void AddToTreeView(Category category)
        {
            MyTreeNode myNewNode = null;

            if (category.ParentId == Guid.Empty)
            {
                myNewNode = m_MyAllFilesNode.Nodes.Add(category.Id, category.Label);
            }
            else
            {
                MyTreeNode parent = m_MyTreeView.FindNode(category.ParentId);
                if (parent == null)
                    return;

                myNewNode = parent.Nodes.Add(category.Id, category.Label);
            }
            myNewNode.Category = category;         

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

        private static void OnHideNodeFromResultsRecurse(ref List<Category> categories, MyTreeNode treeNode)
        {
            m_MyNodesToHideFromResults.Add(treeNode);
            treeNode.BackColor = Color.LightPink;

            categories.Add(treeNode.Category);
            foreach (MyTreeNode tn in treeNode.Nodes)
                OnHideNodeFromResultsRecurse(ref categories, tn);
        }

        private static List<Category> OnHideNodeFromResults(TreeNode treeNode)
        {
            List<Category> categories = new List<Category>();
            OnHideNodeFromResultsRecurse(ref categories, treeNode);
            return categories;
        }

        private static List<Category> OnHideNodeFromResults(MyTreeNode treeNode)
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
            m_MyTreeView = new MyTreeView();
            m_MyTreeView.Dock = DockStyle.Fill;
            m_MyTreeView.AllowDrop = true;

            m_MyTreeView.MouseDown += m_MyTreeView_MouseDown;
            m_MyTreeView.MouseMove += m_MyTreeView_MouseMove;
            m_MyTreeView.NodeSelectionChanged += m_MyTreeView_NodeSelectionChanged;
            m_MyTreeView.MouseWheelOutsideControl += m_MyTreeView_MouseWheelOutsideControl;
            m_MyTreeView.NodeMouseDown += m_MyTreeView_NodeMouseDown;
            m_MyTreeView.NodeTextChanged += m_MyTreeView_NodeTextChanged;
            m_MyTreeView.NodeDragEnter += m_MyTreeView_NodeDragEnter;
            m_MyTreeView.NodeDragDrop += m_MyTreeView_NodeDragDrop;

            controlCollection.Add(m_MyTreeView);            
        }

        static void m_MyTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (Status.ReadOnly)
                return;

            if (Common.IsShiftPressed() && e.Button == MouseButtons.Left)
            {
                MyTreeNode tn = m_MyAllFilesNode;
                m_MyTreeView.DoDragDrop(tn, DragDropEffects.Link);
            }
        }

        static void m_MyTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                ShowContextMenu(e.Location);
        }

        private static void ShowContextMenu(Point location)
        {
            ContextMenu menu = new ContextMenu();
            MenuItem mi = null;

            mi = new MenuItem();
            mi.Text = "Collapse all";
            mi.Name = "CollapseTree";
            mi.Click += new EventHandler(contextMenuItem_Click);
            menu.MenuItems.Add(mi);                   

            menu.Show(m_MyTreeView, location);
        }

        static void m_MyTreeView_NodeDragDrop(MyTreeNode sender, DragEventArgs e)
        {
            if (Status.ReadOnly)
                return;

            if (Status.Busy)
                return;            

            if (e.Data.GetDataPresent(typeof(List<Photo>)))
            {
                if (sender.Category.Id == Guids.AllFiles || sender.Category.Id == Guids.Unassigned)
                    return;

                List<Photo> photos = (List<Photo>)e.Data.GetData(typeof(List<Photo>));

                SetPhotoCategory(photos, sender.Category.Id, ((e.KeyState & KeyStates.Shift) == KeyStates.Shift));
            }
            else if (e.Data.GetDataPresent(typeof(MyTreeNode)))
            {
                if (sender.Category.Id == Guids.Unassigned)
                    return;

                MyTreeNode movingNode = (MyTreeNode)e.Data.GetData(typeof(MyTreeNode));
                Category movingCategory = movingNode.Category;

                if (movingNode == sender || movingNode.Parent == sender || movingNode.Category == null)
                    return;

                if (MessageBox.Show("Do you want to move category " + movingCategory.Name + " under category " + sender.Text + "?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;               

                Database.BeginTransaction();

                RemoveCategoryPathFromBranch(movingNode);

                movingNode.Parent.Nodes.Remove(movingNode, true);
                sender.Nodes.Add(movingNode);

                CreateCategoryPathForBrach(movingNode);

                if (sender.Category.Id == Guids.AllFiles)
                    Database.UpdateCategory(movingCategory.Id, Guid.Empty, movingCategory.Name);
                else
                    Database.UpdateCategory(movingCategory.Id, sender.Category.Id, movingCategory.Name);

                Database.Commit();

                m_MyTreeView.RefreshOrWhatever();
            }
        }

        static void m_MyTreeView_NodeDragEnter(MyTreeNode sender, DragEventArgs e)
        {
            if (sender.Parent.Key != null)
                e.Effect = DragDropEffects.Link;
        }

        static void m_MyTreeView_NodeTextChanged(MyTreeNode node)
        {
            Status.Busy = true;

            Category category = node.Category;
            category.Name = node.Text;
            Database.UpdateCategory(category.Id, category.ParentId, category.Name);
            
            Status.Busy = false;
            Status.LabelEdit = false;
        }

        static void m_MyTreeView_MouseWheelOutsideControl(object sender, short direction)
        {
            Common.MouseWheelOutsideTreeView(direction > 0);
        }        

        //static void m_TreeView_MouseWheelOutsideControl(object sender, short direction)
        //{
        //    Common.MouseWheelOutsideTreeView(direction > 0);
        //}        

        internal static void Populate()
        {
            if (!Database.HasConnection)
                return;

            foreach (DataRow dr in Database.QueryCategories().Rows)
            {
                m_Categories.Add((Guid)dr["CATEGORY_ID"], new Category((Guid)dr["CATEGORY_ID"], (Guid)dr["PARENT_ID"], dr["NAME"].ToString(), dr["COLOR"]));
            }

            m_MyUnassignedNode = m_MyTreeView.Nodes.Add(Guids.Unassigned, Multilingual.GetText("categories", "unassigned", "Unassigned"));
            m_MyAllFilesNode = m_MyTreeView.Nodes.Add(Guids.AllFiles, Multilingual.GetText("categories", "allItems", "All items"));

            foreach (Category category in m_Categories.Values)
                if (category.ParentId == Guid.Empty && category.Id != Guids.AllFiles && category.Id != Guids.Unassigned)
                    AddToTreeView(category);

            m_MyAllFilesNode.Expand();
            
            m_MyTreeView.RefreshOrWhatever();
        }

#endregion

#region events

        static void m_MyTreeView_NodeMouseDown(MyTreeNode node, MouseEventArgs e)
        {
            if (Status.Busy)
                return;

            if (e.Button == MouseButtons.Right)
                ShowContextMenu(e.Location, node);
        }

        //static void m_TreeView_DragDrop(object sender, DragEventArgs e)
        //{
        //    if (Status.ReadOnly)
        //        return;

        //    if (Status.Busy)
        //        return;

        //    Point point = new Point(e.X, e.Y);
        //    point = m_TreeView.PointToClient(point);
        //    TreeNode tn = m_TreeView.GetNodeAt(point);

        //    if (tn == null)
        //        return;

        //    Guid categoryId = new Guid(tn.Name);            

        //    if (e.Data.GetDataPresent(typeof(List<Photo>)))
        //    {
        //        if (categoryId == Guids.AllFiles || categoryId == Guids.Unassigned)
        //            return;

        //        List<Photo> photos = (List<Photo>)e.Data.GetData(typeof(List<Photo>));

        //        SetPhotoCategory(photos, categoryId, ((e.KeyState & KeyStates.Shift) == KeyStates.Shift));                
        //    }
        //    else if (e.Data.GetDataPresent(typeof(TreeNode)))
        //    {
        //        if (categoryId == Guids.Unassigned)
        //            return;                

        //        TreeNode movingNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
        //        Category movingCategory = (Category)movingNode.Tag;

        //        if (movingNode == tn || movingNode.Parent == tn)
        //            return;

        //        if (MessageBox.Show("Do you want to move category " + movingCategory.Name + " under category " + tn.Text + "?", "", MessageBoxButtons.YesNo) == DialogResult.No)
        //            return;

        //        Database.BeginTransaction();

        //        RemoveCategoryPathFromBranch(movingNode);

        //        m_TreeView.Nodes.Remove(movingNode);
        //        tn.Nodes.Add(movingNode);

        //        CreateCategoryPathForBrach(movingNode);

        //        if (categoryId == Guids.AllFiles)
        //            categoryId = Guid.Empty;

        //        Database.UpdateCategory(movingCategory.Id, categoryId, movingCategory.Name);

        //        Database.Commit();
        //    }
        //}

        //static void m_TreeView_DragEnter(object sender, DragEventArgs e)
        //{
        //    e.Effect = DragDropEffects.Link;
        //}

        //static void m_TreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        //{
        //    if (e.Node == m_AllFilesNode)
        //        e.Cancel = true;
        //}

        static void m_MyTreeView_NodeSelectionChanged()
        {
            if (Status.Busy)
                Thumbnails.ClearPhotos();
            else
                FetchPhotos();
        }

#endregion        

        private static void RemoveCategoryPathFromBranch(MyTreeNode node)
        {
            Database.DeleteCategoryPathByCategory(node.Category.Id);

            foreach (MyTreeNode tn in node.Nodes)
                RemoveCategoryPathFromBranch(tn);
        }

        private static void CreateCategoryPathForBrach(MyTreeNode node)
        {            
            foreach (MyTreeNode tn in node.Nodes)
                CreateCategoryPathForBrach(tn);

            Guid categoryId = node.Category.Id;

            Database.InsertCategoryPath(categoryId, categoryId);
            string temp = node.Text;

            while (node.Parent.Category != null)
            {
                node = node.Parent;
                Database.InsertCategoryPath(node.Category.Id, categoryId);
            }
        }

#region context menus

        private static void ShowContextMenu(Point location, MyTreeNode targetNode)
        {
            if (targetNode == null)
                return;

            ContextMenu menu = new ContextMenu();
            MenuItem mi = null;

            bool properTargetNode = (targetNode != null && targetNode != m_MyAllFilesNode && targetNode != m_MyUnassignedNode);

            bool needSeparator = false;

            if (m_MyTreeView.SelectedNodes.Count != 0 && properTargetNode && !m_MyTreeView.SelectedNodes.Contains(targetNode))
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

                    if (m_MyTreeView.SelectedNodes.Count == 1)
                    {
                        menu.MenuItems.Add("-");

                        if (m_MyTreeView.SelectedNodes.Contains(targetNode))
                        {
                            mi = new MenuItem();
                            mi.Text = "Auto categories";
                            mi.Name = "AutoCategories";
                            //mi.Click += new EventHandler(contextMenuItem_Click);
                            menu.MenuItems.Add(mi);
                        }
                        else
                        {
                            mi = new MenuItem();
                            mi.Text = "Add auto category";
                            mi.Name = "AddAutoCategory";
                            mi.Click += new EventHandler(contextMenuItem_Click);
                            menu.MenuItems.Add(mi);
                        }
                    }

                    menu.MenuItems.Add("-");

                    mi = new MenuItem();
                    mi.Text = "Set category color";
                    mi.Name = "SetCategoryColor";
                    mi.Click += new EventHandler(contextMenuItem_Click);
                    menu.MenuItems.Add(mi);

                    if (Status.ShowHiddenCategories)
                    {
                        menu.MenuItems.Add("-");

                        mi = new MenuItem();
                        mi.Text = "test";
                        mi.Name = "test";
                        mi.Click += new EventHandler(contextMenuItem_Click);
                        menu.MenuItems.Add(mi);
                    }
                }
            }

            m_MyMenuTargetNode = targetNode;
            menu.Show(m_MyTreeView, location);
        }

        //private static void ShowContextMenu(Point location, TreeNode targetNode)
        //{
        //    if (targetNode == null)
        //        return;

        //    ContextMenu menu = new ContextMenu();
        //    MenuItem mi = null;

        //    bool properTargetNode = (targetNode != null && targetNode != m_AllFilesNode && targetNode != m_UnassignedNode);

        //    bool needSeparator = false;

        //    if (m_TreeView.SelectedNodes.Count != 0 && properTargetNode && !m_TreeView.SelectedNodes.Contains(targetNode))
        //    {
        //        mi = new MenuItem();
        //        mi.Text = "Hide from results";
        //        mi.Name = "HideFromResults";
        //        mi.Click += new EventHandler(contextMenuItem_Click);
        //        menu.MenuItems.Add(mi);

        //        needSeparator = true;
        //    }

        //    if (targetNode.Nodes.Count != 0)
        //    {
        //        mi = new MenuItem();
        //        mi.Text = "Hide children from results";
        //        mi.Name = "HideChildrenFromResults";
        //        mi.Click += new EventHandler(contextMenuItem_Click);
        //        menu.MenuItems.Add(mi);

        //        needSeparator = true;
        //    }

        //    if (!Status.ReadOnly)
        //    {
        //        if (needSeparator)
        //            menu.MenuItems.Add("-");

        //        mi = new MenuItem();
        //        mi.Text = "New category";
        //        mi.Name = "NewCategory";
        //        mi.Click += new EventHandler(contextMenuItem_Click);
        //        menu.MenuItems.Add(mi);

        //        if (properTargetNode)
        //        {
        //            mi = new MenuItem();
        //            mi.Text = "New category as child";
        //            mi.Name = "NewChildCategory";
        //            mi.Click += new EventHandler(contextMenuItem_Click);
        //            menu.MenuItems.Add(mi);

        //            menu.MenuItems.Add("-");

        //            mi = new MenuItem();
        //            mi.Text = "Rename category";
        //            mi.Name = "RenameCategory";
        //            mi.Click += new EventHandler(contextMenuItem_Click);
        //            menu.MenuItems.Add(mi);

        //            mi = new MenuItem();
        //            mi.Text = "Delete category";
        //            mi.Name = "DeleteCategory";
        //            mi.Click += new EventHandler(contextMenuItem_Click);
        //            menu.MenuItems.Add(mi);

        //            if (m_TreeView.SelectedNodes.Count == 1)
        //            {
        //                menu.MenuItems.Add("-");

        //                if (m_TreeView.SelectedNodes.Contains(targetNode))
        //                {
        //                    mi = new MenuItem();
        //                    mi.Text = "Auto categories";
        //                    mi.Name = "AutoCategories";
        //                    //mi.Click += new EventHandler(contextMenuItem_Click);
        //                    menu.MenuItems.Add(mi);
        //                }
        //                else
        //                {                            
        //                    mi = new MenuItem();
        //                    mi.Text = "Add auto category";
        //                    mi.Name = "AddAutoCategory";
        //                    mi.Click += new EventHandler(contextMenuItem_Click);
        //                    menu.MenuItems.Add(mi);
        //                }
        //            }

        //            menu.MenuItems.Add("-");

        //            mi = new MenuItem();
        //            mi.Text = "Set category color";
        //            mi.Name = "SetCategoryColor";
        //            mi.Click += new EventHandler(contextMenuItem_Click);
        //            menu.MenuItems.Add(mi);
        //        }
        //    }

        //    m_MenuTargetNode = targetNode;
        //    menu.Show(m_TreeView, location);
        //}

        private static void EditCategoryLabel(MyTreeNode treeNode)
        {
            m_MyTreeView.EditLabel(treeNode);
            Status.LabelEdit = true;
        }

        static void contextMenuItem_Click(object sender, EventArgs e)
        {
            Guid newNodeGuid = Guid.Empty;

            switch ((sender as MenuItem).Name)
            {
                case "HideFromResults":
                    Status.Busy = true;
                    Thumbnails.HideCategoriesFromResults(OnHideNodeFromResults(m_MyMenuTargetNode), Guid.Empty);
                    m_MyTreeView.RefreshOrWhatever();
                    Status.Busy = false;
                    break;
                case "HideChildrenFromResults":
                    Status.Busy = true;
                    Color color = m_MyMenuTargetNode.BackColor;
                    List<Category> categories = OnHideNodeFromResults(m_MyMenuTargetNode);
                    categories.Remove(m_MyMenuTargetNode.Category);
                    m_MyMenuTargetNode.BackColor = color;
                    Thumbnails.HideCategoriesFromResults(categories, (m_MyTreeView.SelectedNodes.Contains(m_MyMenuTargetNode) ? (m_MyMenuTargetNode.Category).Id : Guid.Empty));
                    m_MyTreeView.RefreshOrWhatever();
                    Status.Busy = false;
                    break;
                case "NewCategory":
                    if (m_MyMenuTargetNode == null || m_MyMenuTargetNode.Parent == null || m_MyMenuTargetNode.Parent.Category == null)
                        newNodeGuid = AddCategory("New Category", Guid.Empty);
                    else
                        newNodeGuid = AddCategory("New Category", m_MyMenuTargetNode.Parent.Category.Id);
                    break;
                case "NewChildCategory":
                    newNodeGuid = AddCategory("New Category", m_MyMenuTargetNode.Category.Id);
                    m_MyMenuTargetNode.Expand();
                    break;
                case "RenameCategory":
                    EditCategoryLabel(m_MyMenuTargetNode);
                    break;
                case "DeleteCategory":
                    DeleteCategoryByNodeExt(m_MyMenuTargetNode);
                    Thumbnails.ClearPhotos();
                    break;
                case "AddAutoCategory":
                    throw new NotImplementedException();
                //    AddAutoCategory(m_MenuTargetNode.Tag as Category);
                //    break;
                case "SetCategoryColor":                        
                    SetCategoryColor(m_MyMenuTargetNode);
                    break;
                case "CollapseTree":
                    m_MyTreeView.CollapseAll();
                    m_MyAllFilesNode.Expand();
                    m_MyTreeView.RefreshOrWhatever();
                    break;
            }

            if (newNodeGuid != Guid.Empty)
                EditCategoryLabel(m_MyTreeView.FindNode(newNodeGuid));            
        }

        private static void SetCategoryColor(MyTreeNode tn)
        {
            using (ColorDialog cd = new ColorDialog())
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    //if (tn.Nodes.Count > 0)
                    // update child nodes?

                    Status.Busy = true;
                    UpdateCategoryColor(tn, cd.Color, true);
                    Status.Busy = false;
                }
                else
                {
                    // clear color?
                }
        }

        private static void SetCategoryColor(TreeNode tn)
        {
            using (ColorDialog cd = new ColorDialog())
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    //if (tn.Nodes.Count > 0)
                        // update child nodes?

                    Status.Busy = true;
                    UpdateCategoryColor(tn, cd.Color, true);
                    Status.Busy = false;
                }
                else
                {
                    // clear color?
                }
        }

        private static void UpdateCategoryColor(MyTreeNode tn, Color color, bool updateChildren)
        {
            tn.Category.Color = color;
            Database.UpdateCategory(tn.Category);

            if (updateChildren)
                foreach (MyTreeNode node in tn.Nodes)
                    UpdateCategoryColor(node, color, true);
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
            throw new NotImplementedException();

            //if (m_TreeView.SelectedNodes.Count != 1)
            //    return;

            //Category source = (Category)m_TreeView.SelectedNodes[0].Tag;

            //if (MessageBox.Show("Add " + category.Name + " as auto category for " + source.Name + "?", "Add auto category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            //    return;

            //Database.InsertAutoCategory(source.Id, category.Id);
            //Database.ApplyAutoCategories(source.Id);
        }

        private static void DeleteCategoryByNode(TreeNode treeNode)
        {
            foreach (TreeNode tn in treeNode.Nodes)
                DeleteCategoryByNode(tn);

            Category category = (Category)treeNode.Tag;
            Database.DeleteCategory(category.Id);
            m_Categories.Remove(category.Id);
        }

        private static void DeleteCategoryByNode(MyTreeNode treeNode)
        {
            foreach (MyTreeNode tn in treeNode.Nodes)
                DeleteCategoryByNode(tn);

            Category category = treeNode.Category;
            Database.DeleteCategory(category.Id);
            m_Categories.Remove(category.Id);
        }

        //TODO: tyhmä nimi
        private static void DeleteCategoryByNodeExt(MyTreeNode treeNode)
        {
            if (MessageBox.Show("Delete category " + treeNode.Text + "?", "Delete category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            Status.ShowProgress();
            Database.BeginTransaction();

            DeleteCategoryByNode(treeNode);
            m_MyTreeView.RemoveNode(treeNode);

            Database.Commit();
            Status.HideProgress();

            m_MyTreeView.RefreshOrWhatever();
        }

        #endregion

        internal static void Refresh()
        {
            m_MyTreeView.RefreshOrWhatever();
        }
    
        internal static void TakeFocus()
        {           
            m_MyTreeView.Focus();
        }

        internal static void LocateCategory(Guid guid)
        {
            m_MyTreeView.HighlightNode(guid);
        }
    }

    class Category
    {
        internal Guid Id = Guid.Empty;
        internal Guid ParentId = Guid.Empty;
        internal string Name = "";
//        internal long PhotoCount = 0;
        internal Color Color = Color.Black;

        internal string Label
        {
            get { return Name; }
        }

        public Category()
        {
        }

        public Category(Guid id, Guid parentId, string name, object color)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            long colorValue = (color == DBNull.Value ? 0 : (long)color);            
            Color = Color.FromArgb((int)colorValue);
        }
    }
}
