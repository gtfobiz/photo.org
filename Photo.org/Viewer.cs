using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Photo.org
{
    internal static class Viewer
    {
        private static Image m_Image = null;
        private static PictureBox m_PictureBox = new PictureBox();
        private static Label m_FilenameLabel = new Label();
        private static Panel m_CategoriesPanel = new Panel();
        private static Photo m_Photo = null;
        private static bool m_ReadyToDrag = false;
        private static CategoryLabel m_ActiveCategoryLabel = null;
        private static Timer m_SlideShowTimer = new Timer();
        private static Rectangle m_SelectionRectangle = Rectangle.Empty;
        private static decimal m_ZoomFactor = 1;
        private static Point m_ZoomCenter = Point.Empty;
        private static decimal m_AspectRatio = 0;
        private static Point m_PanningOrigin = Point.Empty;
        private static bool m_IsImageDisposable = false;
        //private static bool m_PaintingArea = false;

        /// <summary>
        /// Initializes the Viewer component
        /// </summary>
        /// <param name="controlCollection"></param>
        internal static void Initialize(Control.ControlCollection controlCollection)
        {
            m_PictureBox.Visible = false;            
            m_PictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left;
            m_PictureBox.Location = new Point(0, 25);
            m_PictureBox.Size = new Size(controlCollection.Owner.ClientRectangle.Width, controlCollection.Owner.ClientRectangle.Height - 50);

            m_FilenameLabel.Height = 25;
            m_FilenameLabel.Text = "";
            m_FilenameLabel.Dock = DockStyle.Top;
            m_FilenameLabel.AutoSize = false;
            m_FilenameLabel.Visible = false;

            m_CategoriesPanel.Dock = DockStyle.Bottom;
            m_CategoriesPanel.Visible = false;
            m_CategoriesPanel.Height = 25;

            m_PictureBox.MouseDown += new MouseEventHandler(m_PictureBox_MouseDown);
            m_PictureBox.MouseMove += new MouseEventHandler(m_PictureBox_MouseMove);
            m_PictureBox.MouseUp += m_PictureBox_MouseUp;
            m_PictureBox.MouseLeave += new EventHandler(m_PictureBox_MouseLeave);
            m_PictureBox.MouseDoubleClick += new MouseEventHandler(m_PictureBox_MouseDoubleClick);
            m_PictureBox.Paint += new PaintEventHandler(m_PictureBox_Paint);

            m_SlideShowTimer.Tick += new EventHandler(m_SlideShowTimer_Tick);

            controlCollection.Owner.Resize += new EventHandler(Owner_Resize);

            Categories.CategoryAssignmentChanged += new Categories.CategoryAssignmentChangedDelegate(Categories_CategoryAssignmentChanged);

            controlCollection.Add(m_FilenameLabel);            
            controlCollection.Add(m_CategoriesPanel);            
            controlCollection.Add(m_PictureBox);
        }

        static void m_PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            m_PanningOrigin = Point.Empty;
            m_PictureBox.Cursor = Cursors.Default;
        }
        
        static void m_PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (m_SelectionRectangle != Rectangle.Empty)
                ControlPaint.DrawFocusRectangle(e.Graphics, m_SelectionRectangle);
        }

        static void m_SlideShowTimer_Tick(object sender, EventArgs e)
        {
            ShowNextPhoto(true);
        }

        static void Owner_Resize(object sender, EventArgs e)
        {
            HandleZoomLevel();
            SetSizeMode();
        }

        static void Categories_CategoryAssignmentChanged(Photo photo, Category category, bool added)
        {
            if (Status.ActiveComponent != Component.Viewer)
                return;

            RefreshCategoryLabels();
        }

        internal static bool KeyPreview(KeyEventArgs e)
        {
            if (Status.LabelEdit)
                return false;

            List<Photo> photoList = new List<Photo>();

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    HideViewer();
                    return true;
                case Keys.Escape:
                    HideViewer();
                    return true;
                case Keys.PageDown:
                case Keys.Right:
                case Keys.Down:
                case Keys.N:
                    ShowNextPhoto(true);
                    return true;
                case Keys.PageUp:
                case Keys.Left:
                case Keys.Up:
                case Keys.P:
                    ShowNextPhoto(false);
                    return true;
                case Keys.Space:
                    if (Status.ReadOnly)
                        return false;

                    List<Photo> photos = new List<Photo>();
                    photos.Add(m_Photo);
                    Categories.ShowCategoryDialog(photos);
                    return true;
                case Keys.F1:
                    ShowPhotoInfo();
                    return true;
                case Keys.F8:
                    StartSlideShow();
                    return true; 
                case Keys.H:
                    if (!Status.ReadOnly && e.Control)
                    {
                        HidePhoto(!e.Shift);
                        return true;
                    }
                    return false;
                case Keys.Add:
                    photoList.Add(m_Photo);
                    Categories.SetPhotoCategory(photoList, Categories.LastCategoryId, false);
                    return true;
                case Keys.Subtract:
                    photoList.Add(m_Photo);
                    Categories.SetPhotoCategory(photoList, Categories.LastCategoryId, true);
                    return true;
                case Keys.Insert:
                    Zoom(true);
                    return true;
                case Keys.Delete:
                    Zoom(false);
                    return true;
            }
            return false;
        }

        private static void HidePhoto(bool hide)
        {
            if (hide)
            {
                if (!m_Photo.Categories.Contains(Guids.Hidden))
                {
                    m_Photo.Categories.Add(Guids.Hidden);
                    Database.InsertPhotoCategory(m_Photo.Id, Guids.Hidden);
                }
            }
            else
            {
                if (m_Photo.Categories.Contains(Guids.Hidden))
                {
                    m_Photo.Categories.Remove(Guids.Hidden);
                    Database.DeletePhotoCategory(m_Photo.Id, Guids.Hidden);
                }
            }

            SetCategoriesPanelBackColor();
        }

        private static void StopSlideShow()
        {
            m_SlideShowTimer.Stop();
        }

        private static void StartSlideShow()
        {
            int interval = Common.StrToInt(Settings.Get(Setting.SlideShowTimerInterval));
            if (interval == 0)
            {
                interval = 2000;
                Settings.Set(Setting.SlideShowTimerInterval, interval.ToString());
            }

            ShowNextPhoto(true);
            m_SlideShowTimer.Interval = interval;
            m_SlideShowTimer.Start();
        }

        private static void ShowPhotoInfo()
        {
            m_Photo.ShowInfo();            
        }

        private static void ShowNextPhoto(bool forward)
        {
            Photo photo = Thumbnails.GetNextPhotoToShow(forward);
            if (photo == null)
            {
                StopSlideShow();
                return;
            }
            ShowPhoto(photo);
        }

        internal static void Hide()
        {
            StopSlideShow();
            m_FilenameLabel.Visible = false;
            m_CategoriesPanel.Visible = false;            
            m_PictureBox.Visible = false;

            m_PictureBox.Image = null;
            if (m_Image != null)
                m_Image.Dispose();
        }

        private static void HideViewer()
        {            
            Hide();
            Thumbnails.Show();            
        }

        /// <summary>
        /// Sets image sizing mode based on image dimensions relative to available space.
        /// </summary>
        private static void SetSizeMode()
        {
            if (m_PictureBox.Image == null)
                return;

            SizeF size = m_PictureBox.Image.PhysicalDimension;
            if (size.Width < m_PictureBox.Width && size.Height < m_PictureBox.Height)
                m_PictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
            else
                m_PictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        }

        internal static void HandleZoomLevel()
        {
            if (m_ZoomFactor == 1M)
            {
                m_PictureBox.Image = m_Image;
                m_IsImageDisposable = false;
            }
            else
            {
                Point canvas = new Point(m_PictureBox.Parent.ClientRectangle.Width, m_PictureBox.Parent.ClientRectangle.Height);
                canvas.Y -= m_CategoriesPanel.Height + m_FilenameLabel.Height;

                if (canvas.X / canvas.Y > m_AspectRatio)
                    canvas.X = (int)(canvas.Y * m_AspectRatio);
                else
                    canvas.Y = (int)(canvas.X / m_AspectRatio);

                Bitmap zoomedImage = new Bitmap(canvas.X, canvas.Y, PixelFormat.Format24bppRgb);
                //try
                {
                    using (Graphics g2 = Graphics.FromImage(zoomedImage))
                    {
                        g2.Clear(Color.Cyan);
                        g2.InterpolationMode = InterpolationMode.NearestNeighbor;

                        g2.DrawImage(m_Image, 
                            new Rectangle(0, 0, canvas.X, canvas.Y), 
                            new Rectangle(
                                m_ZoomCenter.X - (int)((decimal)m_Image.Width / m_ZoomFactor / 2), 
                                m_ZoomCenter.Y - (int)((decimal)m_Image.Height / m_ZoomFactor / 2), 
                                (int)((decimal)m_Image.Width / m_ZoomFactor), 
                                (int)((decimal)m_Image.Height / m_ZoomFactor)), 
                            GraphicsUnit.Pixel);

                        if (m_IsImageDisposable && m_PictureBox.Image != null)
                            m_PictureBox.Image.Dispose();

                        m_PictureBox.Image = zoomedImage;
                        m_IsImageDisposable = true;
                    }
                 }
            }

            SetSizeMode();
        }

        internal static void ShowPhoto(Photo photo)
        {
            Status.Busy = true;

            m_Photo = photo;            

            if (!m_Photo.Exists())
            {
                HideViewer();
            }
            else
            {
                if (m_Image != null)
                    m_Image.Dispose();

                m_Image = Image.FromFile(m_Photo.FilenameWithPath);

                m_AspectRatio = (decimal)m_Image.Width / (decimal)m_Image.Height;
                m_ZoomFactor = 1M;
                m_ZoomCenter = new Point(m_Image.Width / 2, m_Image.Height / 2);
                m_PanningOrigin = Point.Empty;
                m_PictureBox.Cursor = Cursors.Default;

                Status.ActiveComponent = Component.Viewer;                

                RefreshCategoryLabels();

                //m_PictureBox.Image = m_Image;
                HandleZoomLevel();                

                SizeF size = m_PictureBox.Image.PhysicalDimension;
                m_FilenameLabel.Text = m_Photo.Filename + "  (" 
                    + size.Width.ToString() + " x " 
                    + size.Height.ToString() + ", " 
                    + Common.GetFileSizeString(m_Photo.FileSize) + ")  [" 
                    + (Thumbnails.CurrentPhotoOrdinalNumber).ToString() + " / " 
                    + Thumbnails.NumberOfPhotos.ToString() + "]";

                m_FilenameLabel.Visible = true;
                m_CategoriesPanel.Visible = true;
                m_PictureBox.Visible = true;
            }

            SetCategoriesPanelBackColor();

            Status.Busy = false;
        }

        private static void SetCategoriesPanelBackColor()
        {
            m_CategoriesPanel.BackColor = (m_Photo.Categories.Contains(Guids.Hidden) ? Color.Pink : SystemColors.Control);
        }

        private static void RefreshCategoryLabels()
        {            
            Label label = null;
            int offset = 0;

            foreach (CategoryLabel cl in m_CategoriesPanel.Controls)
                DeleteCategoryLabel(cl);

            m_CategoriesPanel.Controls.Clear();

            foreach (Guid categoryId in m_Photo.Categories)
                if (categoryId != Guids.Hidden)
                {
                    label = new CategoryLabel(Categories.GetCategoryByGuid(categoryId));

                    label.MouseClick += new MouseEventHandler(label_MouseClick);

                    m_CategoriesPanel.Controls.Add(label);
                    label.Top = 5;
                    label.Left = offset;
                    offset = label.Left + label.Width + 2;
                }

            // temporary implementation
            foreach (Guid categoryId in m_Photo.AutoCategories)
                if (categoryId != Guids.Hidden)
                {
                    label = new CategoryLabel(Categories.GetCategoryByGuid(categoryId));
                    label.Font = new Font(label.Font, FontStyle.Italic);

                    m_CategoriesPanel.Controls.Add(label);
                    label.Top = 5;
                    label.Left = offset;
                    offset = label.Left + label.Width + 2;
                }           
        }

        private static void DeleteCategoryLabel(CategoryLabel categoryLabel)
        {
            categoryLabel.MouseClick -= new MouseEventHandler(label_MouseClick);
            categoryLabel.Dispose();
        }

        static void label_MouseClick(object sender, MouseEventArgs e)
        {
            if (Status.ReadOnly)
                return;

            if (e.Button != MouseButtons.Right || e.Clicks != 1)
                return;

            if (Common.IsShiftPressed())
            {
                RemovePhotoCategory((sender as CategoryLabel).Category.Id);
                return;
            }

            ShowLabelContextMenu(sender as CategoryLabel, e.Location);            
        }

        private static void RemovePhotoCategory(Guid categoryId)
        {
            Categories.RemovePhotoCategory(m_Photo, categoryId);
        }

        private static void ShowLabelContextMenu(CategoryLabel categoryLabel, Point location)
        {
            ContextMenu menu = new ContextMenu();
            MenuItem mi = null;

            mi = new MenuItem();
            mi.Text = Multilingual.GetText("viewerLabelContextMenu", "locateCategory", "Locate category");
            mi.Name = "LocateCategory";
            mi.DefaultItem = true;
            mi.Click += new EventHandler(mi_Click);
            menu.MenuItems.Add(mi);

            mi = new MenuItem();
            mi.Text = Multilingual.GetText("viewerLabelContextMenu", "removeCategory", "Remove category");
            mi.Name = "RemoveCategory";
            mi.Click += new EventHandler(mi_Click);
            menu.MenuItems.Add(mi);            

            m_ActiveCategoryLabel = categoryLabel;
            menu.Show(categoryLabel, location);            
        }

        static void mi_Click(object sender, EventArgs e)
        {
            switch ((sender as MenuItem).Name)
            {
                case "RemoveCategory":
                    RemovePhotoCategory(m_ActiveCategoryLabel.Category.Id);
                    break;
                case "LocateCategory":
                    Categories.LocateCategory(m_ActiveCategoryLabel.Category.Id);
                    break;
            }
            m_ActiveCategoryLabel = null;
        }

#region events   

        static void m_PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                HideViewer();
        }

        static void m_PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)         
                m_ReadyToDrag = true;

            if (e.Button == MouseButtons.Right)
            {
                m_PanningOrigin = e.Location;

                if (m_ZoomFactor == 1M)
                    m_PictureBox.Cursor = Cursors.NoMoveVert;
                else
                    m_PictureBox.Cursor = Cursors.NoMove2D;
            }
        }

        static void m_PictureBox_MouseLeave(object sender, EventArgs e)
        {
            m_ReadyToDrag = false;

            m_PanningOrigin = Point.Empty;
            m_PictureBox.Cursor = Cursors.Default;
        }

        static void m_PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                m_PanningOrigin = Point.Empty;
                m_PictureBox.Cursor = Cursors.Default;
            }

            if (m_PanningOrigin != Point.Empty)
            {
                if (m_ZoomFactor != 1M)
                {
                    m_ZoomCenter.X -= (int)((e.X - m_PanningOrigin.X) / m_ZoomFactor);
                    m_ZoomCenter.Y -= (int)((e.Y - m_PanningOrigin.Y) / m_ZoomFactor);
                    HandleZoomLevel();
                }

                m_PanningOrigin = e.Location;                
            }

            //if (Core.IsCtrlPressed())
            //{
            //    if (m_SelectionRectangle == Rectangle.Empty)
            //    {
            //        m_SelectionRectangle = new Rectangle(e.X, e.Y, 0, 0);
            //    }
            //    else
            //    {
            //        m_SelectionRectangle.Width = e.X - m_SelectionRectangle.Left;
            //        m_SelectionRectangle.Height = e.Y - m_SelectionRectangle.Top;
            //    }

            //    m_PictureBox.Refresh();
            //}

                
            //        m_PaintingArea = false;
            //        m_SelectionRectangle = Rectangle.Empty;
            //    }
            //    else
            //    {
            //        m_SelectionRectangle.Width = e.X - m_SelectionRectangle.Left;
            //        m_SelectionRectangle.Height = e.Y - m_SelectionRectangle.Top;
            //    }
            //}

            if (Status.ReadOnly)
                return;

            if (e.Button == MouseButtons.Left && m_ReadyToDrag)
            {
                List<Photo> photos = new List<Photo>(1);
                photos.Add(m_Photo);
                m_PictureBox.DoDragDrop(photos, DragDropEffects.Link);
            }
        }
#endregion        
    
        internal static void MouseWheelOutsideTreeView(bool mouseWheelForward)
        {
            if (m_PanningOrigin == Point.Empty)
            {
                ShowNextPhoto(!mouseWheelForward);
            }
            else
            {
                Zoom(mouseWheelForward);

                if (m_PanningOrigin != Point.Empty)
                { 
                    if (m_ZoomFactor == 1M)
                        m_PictureBox.Cursor = Cursors.NoMoveVert;
                    else
                        m_PictureBox.Cursor = Cursors.NoMove2D;
                }
            }
        }

        private static void Zoom(bool zoomIn)
        {
            if (zoomIn)
            {
                m_ZoomFactor *= 1.5M;
            }
            else
            {
                if (m_ZoomFactor == 1M)
                    return;

                m_ZoomFactor /= 1.5M;

                if (m_ZoomFactor == 1M)
                    m_ZoomCenter = new Point(m_Image.Width / 2, m_Image.Height / 2);
            }

            HandleZoomLevel();
        }
    }

    internal class CategoryLabel : Label
    {
        private Category m_Category = null;

        internal Category Category
        {
            get { return m_Category; }
            set { m_Category = value; }
        }

        internal CategoryLabel() : base()
        {
            this.AutoSize = true;
        }

        internal CategoryLabel(Category category) : this()
        {
            m_Category = category;

            this.Text = m_Category.Name;
            this.ForeColor = category.Color;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            this.BackColor = Color.White;
            this.ForeColor = Color.Blue;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (this.Parent != null)
                this.BackColor = this.Parent.BackColor;
            this.ForeColor = m_Category.Color;
        }
    }
}
