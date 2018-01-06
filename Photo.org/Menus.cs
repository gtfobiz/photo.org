using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Photo.org
{
    internal static class Menus
    {
        private static MenuStrip m_MenuStrip = new MenuStrip();

        internal static void Initialize(MainForm mainForm)
        {            
            mainForm.Controls.Add(m_MenuStrip);

            // FILE
            ToolStripMenuItem menu = new ToolStripMenuItem(Multilingual.GetText("menu", "file", "&File"));
            m_MenuStrip.Items.Add(menu);
            ToolStripMenuItem parent = menu;

            if (!Status.ReadOnly)
            {
                menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileAddImages", "&Add images..."));
                menu.Name = "File_AddImages";
                menu.Click += new EventHandler(menu_Click);
                parent.DropDownItems.Add(menu);
            }

            //parent.DropDownItems.Add(new ToolStripSeparator());

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileSelectDatabase", "&Select database..."));
            menu.Name = "File_SelectDatabase";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            //menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileRecentlyUsed", "&Recently used"));
            //menu.Name = "File_MRU";
            //parent.DropDownItems.Add(menu);
            ToolStripMenuItem lesserParent = menu;

            //menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileRecentlyUsedClear", "&Clear"));
            //menu.Name = "File_MRU_Clear";
            //menu.Click += new EventHandler(menu_Click);
            //lesserParent.DropDownItems.Add(menu);

            //lesserParent.DropDownItems.Add(new ToolStripSeparator());


            //TODO



            parent.DropDownItems.Add(new ToolStripSeparator());
            
            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileExit", "E&xit"));
            menu.Name = "File_Exit";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            // VIEW
            int initialSortOrder = Common.StrToInt(Settings.Get(Setting.ThumbnailSortBy));
            string initialSortOrderAscending = Settings.Get(Setting.ThumbnailSortByAscending);
            Thumbnails.SetSortBy((SortBy)initialSortOrder);
            Thumbnails.SetSortAscending(initialSortOrderAscending != "0");

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "view", "&View"));
            m_MenuStrip.Items.Add(menu);
            parent = menu;

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrder", "&Order"));
            menu.Name = "View_ThumbnailOrder";
            parent.DropDownItems.Add(menu);
            lesserParent = menu;

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderFilename", "File&name"));
            menu.Name = "View_ThumbnailOrder_Filename";
            menu.Checked = (initialSortOrder == (int)SortBy.Filename);
            menu.Click += new EventHandler(SetThumbnailOrder);            
            lesserParent.DropDownItems.Add(menu);            

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderFolder", "&Folder"));
            menu.Name = "View_ThumbnailOrder_Folder";
            menu.Checked = (initialSortOrder == (int)SortBy.Folder);
            menu.Click += new EventHandler(SetThumbnailOrder);            
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderFilesize", "&Filesize"));
            menu.Name = "View_ThumbnailOrder_Filesize";
            menu.Checked = (initialSortOrder == (int)SortBy.Filesize);
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderImportDate", "&Import date"));
            menu.Name = "View_ThumbnailOrder_ImportDate";
            menu.Checked = (initialSortOrder == (int)SortBy.ImportDate);
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderResolution", "&Resolution"));
            menu.Name = "View_ThumbnailOrder_Resolution";
            menu.Checked = (initialSortOrder == (int)SortBy.Resolution);
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderWidth", "&Width"));
            menu.Name = "View_ThumbnailOrder_Width";
            menu.Checked = (initialSortOrder == (int)SortBy.Width);
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderHeight", "&Height"));
            menu.Name = "View_ThumbnailOrder_Height";
            menu.Checked = (initialSortOrder == (int)SortBy.Height);
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderRandom", "&Random"));
            menu.Name = "View_ThumbnailOrder_Random";
            menu.Checked = (initialSortOrder == (int)SortBy.Random);
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            lesserParent.DropDownItems.Add("-");

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderAscending", "&Ascending"));
            menu.Name = "View_ThumbnailOrder_Ascending";
            menu.Checked = (initialSortOrderAscending != "0");
            menu.Click += new EventHandler(SetThumbnailOrder);            
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderDescending", "&Descending"));
            menu.Name = "View_ThumbnailOrder_Descending";
            menu.Checked = (initialSortOrderAscending == "0");
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            // TOOLS
            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "tools", "&Tools"));
            m_MenuStrip.Items.Add(menu);
            parent = menu;

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsSearchByFilename", "Search by &filename"));
            menu.Name = "Tools_SearchByFilename";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            if (!Status.ReadOnly)
            {
                menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsSearchByGuid", "Search by &guid"));
                menu.Name = "Tools_SearchByGuid";
                menu.Click += new EventHandler(menu_Click);
                parent.DropDownItems.Add(menu);

                menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsFindDuplicateHashes", "Find &duplicate hashes"));
                menu.Name = "Tools_FindDuplicateHashes";
                menu.Click += new EventHandler(menu_Click);
                parent.DropDownItems.Add(menu);
            }

            if (Status.ShowHiddenPhotos)
            {
                menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsFetchHiddenPhotos", "Fetch &hidden photos"));
                menu.Name = "Tools_FetchHiddenPhotos";
                menu.Click += new EventHandler(menu_Click);
                parent.DropDownItems.Add(menu);
            }

            if (!Status.ReadOnly)
            {
                menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsMaintenance", "&Maintenance"));
                menu.Name = "Tools_Maintenance";
                menu.Click += new EventHandler(menu_Click);
                parent.DropDownItems.Add(menu);

                menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsRepairAutoCategories", "&Repair auto categories"));
                menu.Name = "Tools_RepairAutoCategories";
                menu.Click += new EventHandler(menu_Click);
                parent.DropDownItems.Add(menu);

                menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsUpdateRootFolder", "&Update root folder"));
                menu.Name = "Tools_UpdateRootFolder";
                menu.Click += new EventHandler(menu_Click);
                parent.DropDownItems.Add(menu);
            }

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsStatistics", "&Statistics"));
            menu.Name = "Tools_Statistics";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            // CATEGORY
            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "category", "&Category"));
            m_MenuStrip.Items.Add(menu);
            parent = menu;

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "categorySelect", "&Select..."));
            menu.Name = "Category_Select";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "categoryRequire", "&Require..."));
            menu.Name = "Category_Require";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "categoryHide", "&Hide..."));
            menu.Name = "Category_Hide";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            parent.DropDownItems.Add("-");

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "categoryLocate", "&Locate..."));
            menu.Name = "Category_Locate";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);
        }

        private static void SetThumbnailOrder(object sender, EventArgs e)
        {
            ToolStripMenuItem checkedItem = (ToolStripMenuItem)sender;

            if (checkedItem.Name == "View_ThumbnailOrder_Ascending" || checkedItem.Name == "View_ThumbnailOrder_Descending")
            {
                foreach (object o in (checkedItem.OwnerItem as ToolStripMenuItem).DropDownItems)
                    if (o.GetType().ToString() == "System.Windows.Forms.ToolStripMenuItem")
                    {
                        ToolStripMenuItem item = (ToolStripMenuItem)o;
                        if (item.Name == "View_ThumbnailOrder_Ascending" || item.Name == "View_ThumbnailOrder_Descending")
                            item.Checked = (item == checkedItem);
                    }
            }
            else
            {
                foreach (object o in (checkedItem.OwnerItem as ToolStripMenuItem).DropDownItems)
                    if (o.GetType().ToString() == "System.Windows.Forms.ToolStripMenuItem")
                    {
                        ToolStripMenuItem item = (ToolStripMenuItem)o;
                        if (item.Name != "View_ThumbnailOrder_Ascending" && item.Name != "View_ThumbnailOrder_Descending")
                            item.Checked = (item == checkedItem);
                    }
            }

            switch (checkedItem.Name)
            {
                case "View_ThumbnailOrder_Ascending":
                    Thumbnails.SetSortAscending(true);
                    break;
                case "View_ThumbnailOrder_Descending":
                    Thumbnails.SetSortAscending(false);
                    break;
                case "View_ThumbnailOrder_Filename":
                    Thumbnails.SetSortBy(SortBy.Filename);
                    break;
                case "View_ThumbnailOrder_Folder":
                    Thumbnails.SetSortBy(SortBy.Folder);
                    break;
                case "View_ThumbnailOrder_Filesize":
                    Thumbnails.SetSortBy(SortBy.Filesize);
                    break;
                case "View_ThumbnailOrder_ImportDate":
                    Thumbnails.SetSortBy(SortBy.ImportDate);
                    break;
                case "View_ThumbnailOrder_Resolution":
                    Thumbnails.SetSortBy(SortBy.Resolution);
                    break;
                case "View_ThumbnailOrder_Width":
                    Thumbnails.SetSortBy(SortBy.Width);
                    break;
                case "View_ThumbnailOrder_Height":
                    Thumbnails.SetSortBy(SortBy.Height);
                    break;
                case "View_ThumbnailOrder_Random":
                    Thumbnails.SetSortBy(SortBy.Random);
                    break;
            }
        }

        private static void menu_Click(object sender, EventArgs e)
        {
            switch (((ToolStripMenuItem)sender).Name)
            {
                case "File_SelectDatabase":
                    Database.SelectDatabaseFile();
                    Common.OnDatabaseChange();
                    break;
                case "File_AddImages":
                    if (!Status.ReadOnly)
                        Thumbnails.ShowImportFolderDialog();
                    break;
                case "File_Exit":
                    Common.Exit();
                    break;
                case "Tools_Maintenance":
                    if (!Status.ReadOnly)
                        Database.DoMaintenance();
                    break;
                case "Tools_RepairAutoCategories":
                    if (!Status.ReadOnly)
                        Database.RepairAutoCategories();
                    break;
                case "Tools_Statistics":
                    Database.ShowStatistics();
                    break;                
                case "Tools_FetchHiddenPhotos":
                    Thumbnails.FetchHiddenPhotos();
                    break;
                case "Tools_SearchByFilename":
                    Thumbnails.SearchByFilename();
                    break;
                case "Tools_SearchByGuid":
                    Thumbnails.SearchByGuid();
                    break;
                case "Tools_FindDuplicateHashes":
                    Thumbnails.FindDuplicateHashes();
                    break;                    
                case "Tools_UpdateRootFolder":
                    UpdateRootFolder();
                    break;
                case "Category_Select":
                    Categories.ShowCategoryDialog(CategoryDialogMode.Select);
                    break;
                case "Category_Require":
                    Categories.ShowCategoryDialog(CategoryDialogMode.Require);
                    break;
                case "Category_Hide":
                    Categories.ShowCategoryDialog(CategoryDialogMode.Hide);
                    break;
                case "Category_Locate":
                    Categories.ShowCategoryDialog(CategoryDialogMode.Locate);
                    break;
            }
        }

        private static void UpdateRootFolder()
        {
            (new UpdateRootForm()).ShowDialog();
        }

        /// <summary>
        /// Sets menu enabled property
        /// </summary>
        /// <param name="enabled"></param>
        internal static void SetEnabled(bool enabled)
        {
            m_MenuStrip.Enabled = enabled;
        }

        internal static void SetVisible(bool visible)
        {
            m_MenuStrip.Visible = visible;
        }
    }
}
