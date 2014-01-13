using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace Photo.org
{
    internal static class Thumbnails
    {
        internal static int ThumbnailSize = 0;

        private static MyListView m_ThumbnailView = null; 
        private static int m_CurrentlyShownPhotoIndex = 0;

        public static int CurrentPhotoOrdinalNumber
        {
            get { return m_ThumbnailView.SelectedIndices[0] + 1; }
        }

        public static int NumberOfPhotos
        {
            get { return m_ThumbnailView.Photos.Count; }
        }

        private static List<string> m_CurrentlyVisible = new List<string>();
        private static Point m_MouseDownLocation = Point.Empty;

        private static readonly object m_Locker = new object();

        private delegate void WorkDoneDelegate(Dictionary<string, Image> thumbnails, bool checkInvoke);
        private delegate Photo GetNextPhotoForLoaderDelegate(Guid batchId, bool checkInvoke);

        /// <summary>
        /// Initializes the thumbnail list component.
        /// </summary>
        /// <param name="splitContainer">Splitcontainer to contain the listview control</param>
        internal static void Initialize(Control.ControlCollection controlCollection)
        {
            ThumbnailSize = Common.StrToInt(Settings.Get(Setting.ThumbnailSize));
            if (ThumbnailSize == 0)
            {
                ThumbnailSize = 100;
                Settings.Set(Setting.ThumbnailSize, ThumbnailSize.ToString());
            }

            m_ThumbnailView = new MyListView();
            m_ThumbnailView.PhotoMouseDown += new MyListView.PhotoMouseDownHandler(m_ThumbnailView_PhotoMouseDown);
            m_ThumbnailView.PhotoMouseUp += new MyListView.PhotoMouseUpHandler(m_ThumbnailView_PhotoMouseUp);
            m_ThumbnailView.PhotoMouseMove += new MyListView.PhotoMouseMoveHandler(m_ThumbnailView_MouseMove);
            m_ThumbnailView.MouseDown += new MouseEventHandler(m_ThumbnailView_MouseDown);
            m_ThumbnailView.MouseMove += new MouseEventHandler(m_ThumbnailView_MouseMove);

            m_ThumbnailView.Dock = DockStyle.Fill;
            controlCollection.Add(m_ThumbnailView); 

            Viewer.Initialize(controlCollection);

            Show();
        }

        static void m_ThumbnailView_MouseDown(object sender, MouseEventArgs e)
        {
            //MessageBox.Show("!!");
        }

        static void m_ThumbnailView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Status.ReadOnly && e.Button == MouseButtons.Left)
            {
                if (m_ThumbnailView.SelectedItems.Count == 0)
                    return;

                List<Photo> photos = new List<Photo>(m_ThumbnailView.SelectedItems.Count);
                foreach (Photo p in m_ThumbnailView.SelectedItems)
                {
                    photos.Add(p);
                }
                m_ThumbnailView.DoDragDrop(photos, DragDropEffects.Link);
            }
        }

        static void m_ThumbnailView_PhotoMouseUp(Photo photo, MouseEventArgs e)
        {
            m_MouseDownLocation = Point.Empty;
        }             

        static void m_ThumbnailView_PhotoMouseDown(Photo photo, MouseEventArgs e)
        {
            m_MouseDownLocation = e.Location;

            if (e.Button == MouseButtons.Left && e.Clicks > 1)
            {
                ShowPhoto(photo);            
            }
            else if (e.Button == MouseButtons.Right && e.Clicks == 1)
            {
                //ShowContextMenu(new Point(10, 10), photo);
                ShowContextMenu(e.Location, photo);
            }
        }             

        /// <summary>
        /// Shows thumbnail list and sets the active component.
        /// </summary>
        internal static void Show()
        {
            m_ThumbnailView.Visible = true;
            Status.ActiveComponent = Component.Photos;
        }

        /// <summary>
        /// Previews key press before passing it to other components.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        internal static bool KeyPreview(KeyEventArgs e)
        {
            if (Status.LabelEdit)
                return false;

            if (m_ThumbnailView.KeyPreview(e))
                return true;

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (m_ThumbnailView.SelectedItems.Count > 0)
                        ShowPhoto(m_ThumbnailView.SelectedItems[0]);
                    return true;
                case Keys.Space:
                    if (Status.ReadOnly)
                        return false;
            //        if (Status.LabelEdit)
            //            return false;

                    List<Photo> photos = new List<Photo>();
                    foreach (Photo photo in m_ThumbnailView.SelectedItems)
                        photos.Add(photo);
                    Categories.ShowCategoryDialog(photos);
                    return true;
                case Keys.F1:
                    return ShowPhotoInfo();
                case Keys.F2:
                    ShowRenameFileDialog();
                    return true;
                case Keys.F3:
                    PrintMD5Hash(@"C:\Users\Jarno\Desktop\selvitys\1sohvakuva-170405.jpg");
                    PrintMD5Hash(@"C:\Users\Jarno\Desktop\selvitys\2sohvakuva-170405.jpg");
                    PrintMD5Hash(@"C:\Users\Jarno\Desktop\selvitys\3sohvakuva-170405.jpg");
                //    Core.ShowAllExif(@"C:\Users\Jarno\Desktop\selvitys\kuva-170405.jpg");
                    return true;
                case Keys.F:
                    if (e.Control)
                    {
                        OpenFilterDialog();
                        return true;
                    }
                    return false;
                case Keys.H:
                    if (!Status.ReadOnly && e.Control)
                    {
                        HideSelectedPhotos(!e.Shift);
                        return true;   
                    }
                    return false;
                case Keys.Delete:
                    RemoveSelectedPhotos();
                    break;
            }

            return false;
        }

        // temp test
        private static void PrintMD5Hash(string p)
        {
            Image image = Image.FromFile(p);
            System.Diagnostics.Trace.WriteLine(Common.GetMD5HashForImage(image));
        }

        private static void ShowRenameFileDialog()
        {
            if (m_ThumbnailView.SelectedItems.Count != 1)
                return;

            using (RenameFileForm f = new RenameFileForm())
            {
                f.Photo = m_ThumbnailView.SelectedItems[0];
                f.ShowDialog();
            }

            m_ThumbnailView.RefreshOrWhatever();
        }

        private static void HideSelectedPhotos(bool hide)
        {            
            if (hide)
            {
                foreach (Photo photo in m_ThumbnailView.SelectedItems)
                    if (!photo.Categories.Contains(Guids.Hidden))
                    {
                        photo.Categories.Add(Guids.Hidden);
                        Database.InsertPhotoCategory(photo.Id, Guids.Hidden);
                    }
            }
            else
            {
                foreach (Photo photo in m_ThumbnailView.SelectedItems)
                    if (photo.Categories.Contains(Guids.Hidden))
                    {
                        photo.Categories.Remove(Guids.Hidden);
                        Database.DeletePhotoCategory(photo.Id, Guids.Hidden);
                    }
            }
        }

        private static void OpenFilterDialog()
        {
            using (FilterForm f = new FilterForm())
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    System.Diagnostics.Trace.WriteLine("filter text: " + f.FilterText);
                    m_ThumbnailView.BeginUpdate();
                    for (int i = m_ThumbnailView.Photos.Count - 1; i >= 0; i--)
                        if (!m_ThumbnailView.Photos[i].FilenameWithPath.Contains(f.FilterText))
                            m_ThumbnailView.Photos.Remove(m_ThumbnailView.Photos[i]);
                    m_ThumbnailView.EndUpdate();

                    ShowPhotoCount();
                }
            }
        }

        private static bool ShowPhotoInfo()
        {
            if (m_ThumbnailView.SelectedItems.Count != 1)
                return false;

            m_ThumbnailView.SelectedItems[0].ShowInfo();

            return true;
        }
        
        internal static void HideCategoriesFromResults(List<Category> categories, Guid unlessContainsThisCategory)
        {
            Show();

            m_ThumbnailView.BeginUpdate();

            Photo photo = null;
            for (int i=m_ThumbnailView.Photos.Count - 1; i >= 0; i--)            
            {
                photo = m_ThumbnailView.Photos[i];

                if (photo.Categories.Contains(unlessContainsThisCategory))
                    continue;

                foreach (Category category in categories)
                    if (photo.Categories.Contains(category.Id) || photo.AutoCategories.Contains(category.Id))
                        m_ThumbnailView.Photos.Remove(photo);
            }

            m_ThumbnailView.EndUpdate();

            ShowPhotoCount();
        }        

        /// <summary>
        /// Imports photos from given folder.
        /// </summary>
        /// <param name="folder">folder to get photos from</param>
        private static void ImportFolder(string folder)
        {
            int i = 0;
            FileInfo fi = null;
            string extension = "";

            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

            Status.ShowProgress(0, files.Length, 0);
            //m_ThumbnailView.BeginUpdate();
            foreach (string file in files)
            {
                fi = new FileInfo(file);
                extension = fi.Extension.ToLower();
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif" || extension == ".png")
                {
                    new Photo(file);
                }
                Status.SetProgress(++i);
                Application.DoEvents();
            }
            //m_ThumbnailView.EndUpdate();
            Status.HideProgress();
        }

        static internal void ShowImportFolderDialog()
        {
            if (!Database.HasConnection)
                return;

            using (FolderBrowserDialog f = new FolderBrowserDialog())
            {
                f.Description = Multilingual.GetText("importFolderDialog", "description", "Select folder to import images from");
                f.RootFolder = Environment.SpecialFolder.Desktop;

                f.SelectedPath = Settings.ImportPath;
                if (f.SelectedPath == "")
                    f.SelectedPath = Environment.SpecialFolder.MyComputer.ToString();

                if (f.ShowDialog() == DialogResult.OK)
                {
                    Database.SaveParameterText("IMPORT_PATH", f.SelectedPath);
                    Settings.ImportPath = f.SelectedPath;

                    Import.ImportFolder(f.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Returns the next listviewitem to show in the viewer component.
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        internal static Photo GetNextPhotoToShow(bool forward)
        {
            if (m_ThumbnailView.SelectedItems.Count == 1)
            {
                int selectedIndex = m_ThumbnailView.SelectedIndices[0];

                while (true)
                {
                    if (forward && selectedIndex + 1 == m_ThumbnailView.Photos.Count)
                        return null;
                    if (!forward && selectedIndex == 0)
                        return null;

                    selectedIndex += (forward ? 1 : -1);

                    if (m_ThumbnailView.Photos[selectedIndex].Exists())
                    {
                        m_ThumbnailView.SelectedIndex = selectedIndex;
                        return m_ThumbnailView.Photos[selectedIndex];
                    }
                }                
            }
            else
            {
                int i = 0;
                while (i < m_ThumbnailView.SelectedItems.Count)
                {
                    m_CurrentlyShownPhotoIndex += (forward ? 1 : -1);
                    if (m_CurrentlyShownPhotoIndex < 0)
                        m_CurrentlyShownPhotoIndex = m_ThumbnailView.SelectedItems.Count - 1;
                    else if (m_CurrentlyShownPhotoIndex >= m_ThumbnailView.SelectedItems.Count)
                        m_CurrentlyShownPhotoIndex = 0;

                    if (m_ThumbnailView.SelectedItems[m_CurrentlyShownPhotoIndex].Exists())
                        return m_ThumbnailView.SelectedItems[m_CurrentlyShownPhotoIndex];

                    i++;
                }
            }

            return null;
        }

        /// <summary>
        /// Activates the viewer component and hides the thumbnail view.
        /// </summary>
        /// <param name="item"></param>
        private static void ShowPhoto(Photo photo)
        {
            if (!photo.Exists())
                return;

            m_CurrentlyShownPhotoIndex = 0;
            Viewer.ShowPhoto(photo);
            m_ThumbnailView.Visible = false;
        }

        /// <summary>
        /// Clears thumbnails and the work list.
        /// </summary>
        internal static void ClearPhotos()
        {
            Worklist.Clear();
            m_ThumbnailView.Clear();
            m_ThumbnailView.RefreshOrWhatever();
            //m_ThumbnailView.Items.Clear();
        }

        /// <summary>
        /// Adds a photo to the listview
        /// </summary>
        /// <param name="filename">filename and path of the photo</param>
        internal static void AddPhoto(Photo photo)
        {
            try
            {
                m_ThumbnailView.Photos.Add(photo);
            }
            catch
            { 
            }
        }
      
        /// <summary>
        /// Fetches photos that belong to all required categories.
        /// </summary>
        /// <param name="required"></param>
        internal static void FetchPhotos(List<Guid> required)
        {
            Viewer.Hide();
            Show();

            Photo photo = null;
            int i = 0;

            Status.ShowProgress();
            Status.SetText("fetching items...");

            ClearPhotos();           

            DataSet ds = Database.QueryPhotosByCategories(required);

            DataRowCollection drc = ds.Tables["Photos"].Rows;
            Status.SetMaxValue(drc.Count);

            m_ThumbnailView.BeginUpdate();
            m_ThumbnailView.Clear();

            foreach (DataRow dr in drc)
            {
                photo = new Photo();
                photo.Id = new Guid(dr["PHOTO_ID"].ToString());
                photo.Path = dr["PATH"].ToString();
                photo.Filename = dr["FILENAME"].ToString();
                photo.FileSize = long.Parse(dr["FILESIZE"].ToString());
                photo.ImportDate = DateTime.Parse(dr["IMPORT_DATE"].ToString());
                photo.Categories = new List<Guid>();
                photo.AutoCategories = new List<Guid>();                

                foreach (DataRow categoryRow in ds.Tables["Categories"].Select("PHOTO_ID = '" + photo.Id.ToString() + "'"))
                {
                    Guid guid = new Guid(categoryRow["CATEGORY_ID"].ToString());

                    if (categoryRow["SOURCE"].ToString() == "U")
                        photo.Categories.Add(guid);
                    else
                        photo.AutoCategories.Add(guid);

                    //if (guid != Guids.Hidden)
                    //    Categories.GetCategoryByGuid(guid).PhotoCount++;
                }

                AddPhoto(photo);
                Status.SetProgress(++i);            
            }

            m_ThumbnailView.EndUpdate();

            Status.HideProgress();
            ShowPhotoCount();
        }

        private static void ShowPhotoCount()
        {
            Status.SetText(m_ThumbnailView.Photos.Count + " items");
        }

#region context menus

        private static void ShowContextMenu(Point location, Photo photo)
        {
            ContextMenu menu = new ContextMenu();
            MenuItem mi = null;
            bool addSeparator = false;            

            if (m_ThumbnailView.SelectedItems.Count == 1)
            {
                mi = new MenuItem();
                mi.Text = Multilingual.GetText("thumbnailsContextMenu", "renameFile", "Rename file");
                mi.Name = "RenameFile";
                mi.Click += new EventHandler(contextMenuItem_Click);
                menu.MenuItems.Add(mi);

                mi = new MenuItem();
                mi.Text = Multilingual.GetText("thumbnailsContextMenu", "openContainingFolder", "Open containing folder");
                mi.Name = "OpenContainingFolder";
                mi.Click += new EventHandler(contextMenuItem_Click);
                menu.MenuItems.Add(mi);

                addSeparator = true;
            }

            if (m_ThumbnailView.SelectedItems.Count > 0)
            {
                mi = new MenuItem();
                mi.Text = Multilingual.GetText("thumbnailsContextMenu", "copySelectedPhotos", "Copy selected photos");
                mi.Name = "CopySelectedPhotos";
                mi.Click += new EventHandler(contextMenuItem_Click);
                menu.MenuItems.Add(mi);

                addSeparator = true;
            }

            //if (m_ListView.SelectedItems.Count > 0)
            //{
            //    if (addSeparator)
            //    {
            //        menu.MenuItems.Add("-");
            //        addSeparator = false;
            //    }

            //    mi = new MenuItem();
            //    mi.Text = Multilingual.GetText("thumbnailsContextMenu", "removeFromDatabase", "Remove from database");
            //    mi.Name = "RemoveFromDatabase";
            //    mi.Click += new EventHandler(contextMenuItem_Click);
            //    menu.MenuItems.Add(mi);
            //}

            if (addSeparator)                
                menu.MenuItems.Add("-");

            mi = new MenuItem();
            mi.Text = Multilingual.GetText("thumbnailsContextMenu", "filterMissingFiles", "Filter missing files");
            mi.Name = "FilterMissingFiles";
            mi.Click += new EventHandler(contextMenuItem_Click);
            menu.MenuItems.Add(mi);
 
            menu.Show(m_ThumbnailView, location);
        }

        static void contextMenuItem_Click(object sender, EventArgs e)
        {            
            switch ((sender as MenuItem).Name)
            {
            //    case "RemoveFromDatabase":
            //        RemoveSelectedPhotos();
            //        break;
                case "RenameFile":
                    ShowRenameFileDialog();
                    break;
                case "OpenContainingFolder":
                    OpenContainingFolder();
                    break;
                case "FilterMissingFiles":
                    FilterMissingFiles();
                    break;
                case "CopySelectedPhotos":
                    CopySelectedPhotos();   
                    break;
            }
        }

        private static void CopySelectedPhotos()
        {            
            using (FolderBrowserDialog f = new FolderBrowserDialog())
            {                
                f.ShowNewFolderButton = true;
                f.Description = "Select destination folder for the copies of the selected photos.";

                if (f.ShowDialog() != DialogResult.OK)
                    return;

                Status.Busy = true;

                string path = f.SelectedPath;
                if (!path.EndsWith(@"\"))
                    path += @"\";                

                foreach (Photo photo in m_ThumbnailView.SelectedItems)
                {
                    FileInfo fi = new FileInfo(photo.FilenameWithPath);
                    string filename = photo.Filename;
                    int postfix = 1;

                    while (File.Exists(path + filename))
                    {
                        filename = photo.Filename.Substring(0, photo.Filename.Length - fi.Extension.Length) + "(" + postfix++.ToString() + ")" + fi.Extension;
                    }

                    File.Copy(photo.FilenameWithPath, path + filename);
                }
            
                Status.Busy = false;

                System.Diagnostics.Process.Start(f.SelectedPath);
            }
        }

        private static void FilterMissingFiles()
        {
            Status.Busy = true;
            for (int i = m_ThumbnailView.Photos.Count-1; i >= 0; i--)
                if (m_ThumbnailView.Photos[i].Exists())
                    m_ThumbnailView.Photos.RemoveAt(i);
            m_ThumbnailView.RefreshOrWhatever();
            Status.Busy = false;
        }

        private static void OpenContainingFolder()
        {
            if (m_ThumbnailView.SelectedItems.Count != 1)
                return;

            System.Diagnostics.Process.Start(m_ThumbnailView.SelectedItems[0].Path);
        }        

        private static void RemoveSelectedPhotos()
        {
            bool recycle = Common.IsShiftPressed();

            if (recycle)
            {
                if (MessageBox.Show("Send " + m_ThumbnailView.SelectedItems.Count.ToString() + " photo(s) to recycle bin?", "Send to recycle bin", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;
            }
            else
            {
                if (MessageBox.Show("Remove " + m_ThumbnailView.SelectedItems.Count.ToString() + " photo(s) from database?", "Remove from database", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;
            }

            Status.Busy = true;

            m_ThumbnailView.BeginUpdate();

            foreach (Photo p in m_ThumbnailView.SelectedItems)
            {
                p.Remove(recycle);
                m_ThumbnailView.Photos.Remove(p);
            }

            m_ThumbnailView.EndUpdate();

            Status.Busy = false;
        }
#endregion

        internal static void SetSortAscending(bool ascending)
        {
            Settings.Set(Setting.ThumbnailSortByAscending, (ascending ? "1" : "0"));
            m_ThumbnailView.SortAscending = ascending;
            m_ThumbnailView.Sort();
            m_ThumbnailView.RefreshOrWhatever();
            Show();
        }

        internal static void SetSortBy(SortBy sortBy)
        {
            Settings.Set(Setting.ThumbnailSortBy, ((int)sortBy).ToString());
            m_ThumbnailView.SortBy = sortBy;
            m_ThumbnailView.Sort();
            m_ThumbnailView.RefreshOrWhatever();
            Show();
        }

        internal static void MouseWheelOutsideTreeView(bool mouseWheelForward)
        {
            m_ThumbnailView.Scroll(!mouseWheelForward, false);
        }
    }
}
