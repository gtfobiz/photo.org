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

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileAddImages", "&Add images..."));
            menu.Name = "File_AddImages";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);            

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileSelectDatabase", "&Select database..."));
            menu.Name = "File_SelectDatabase";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);            

            parent.DropDownItems.Add(new ToolStripSeparator());
            
            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "fileExit", "E&xit"));
            menu.Name = "File_Exit";
            menu.Click += new EventHandler(menu_Click);
            parent.DropDownItems.Add(menu);

            // VIEW
            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "view", "&View"));
            m_MenuStrip.Items.Add(menu);
            parent = menu;

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrder", "&Order"));
            menu.Name = "View_ThumbnailOrder";
            parent.DropDownItems.Add(menu);
            ToolStripMenuItem lesserParent = menu;

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderFilename", "File&name"));
            menu.Name = "View_ThumbnailOrder_Filename";
            menu.Click += new EventHandler(SetThumbnailOrder);
            menu.Checked = true;
            lesserParent.DropDownItems.Add(menu);            

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderFolder", "&Folder"));
            menu.Name = "View_ThumbnailOrder_Folder";
            menu.Click += new EventHandler(SetThumbnailOrder);            
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderFilesize", "&Filesize"));
            menu.Name = "View_ThumbnailOrder_Filesize";
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderImportDate", "&Import date"));
            menu.Name = "View_ThumbnailOrder_ImportDate";
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderRandom", "&Random"));
            menu.Name = "View_ThumbnailOrder_Random";
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            lesserParent.DropDownItems.Add("-");

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderAscending", "&Ascending"));
            menu.Name = "View_ThumbnailOrder_Ascending";
            menu.Click += new EventHandler(SetThumbnailOrder);
            menu.Checked = true;
            lesserParent.DropDownItems.Add(menu);

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "viewThumbnailOrderDescending", "&Descending"));
            menu.Name = "View_ThumbnailOrder_Descending";
            menu.Click += new EventHandler(SetThumbnailOrder);
            lesserParent.DropDownItems.Add(menu);

            // TOOLS
            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "tools", "&Tools"));
            m_MenuStrip.Items.Add(menu);
            parent = menu;

            menu = new ToolStripMenuItem(Multilingual.GetText("menu", "toolsMaintenance", "&Maintenance"));
            menu.Name = "Tools_Maintenance";
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
            }
        }

        /// <summary>
        /// Sets menu enabled property
        /// </summary>
        /// <param name="enabled"></param>
        internal static void SetEnabled(bool enabled)
        {
            m_MenuStrip.Enabled = enabled;
        }
    }
}
