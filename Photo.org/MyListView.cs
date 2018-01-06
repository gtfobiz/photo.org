using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Photo.org
{
    internal class MyListView : UserControl
    {
        internal List<Photo> Photos
        {
            get { return m_Photos; }
            set { m_Photos = value; }
        }

        internal int SelectedIndex
        {
            set 
            {
                m_SelectedItems.Clear();
                if (value >= 0 && m_Photos.Count > 0)
                    m_SelectedItems.Add(m_Photos[value]);

                RefreshOrWhatever();
            }
        }

        internal List<int> SelectedIndices
        {
            get
            {
                List<int> selectedIndices = new List<int>();

                Photo photo = null;

                for (int i = 0; i < m_Photos.Count; i++)
                {
                    photo = m_Photos[i];
                    if (m_SelectedItems.Contains(photo))
                        selectedIndices.Add(i);
                }

                return selectedIndices;
            }
        }

        internal List<Photo> SelectedItems
        {
            get 
            {
                List<Photo> selectedItems = new List<Photo>();
                Photo photo = null;

                for (int i = m_SelectedItems.Count - 1; i >= 0; i--)
                {
                    photo = m_SelectedItems[i];
                    if (m_Photos.Contains(photo))
                        selectedItems.Add(photo);
                    else
                        m_SelectedItems.Remove(photo);
                }

                return selectedItems;
            }
        }

        internal static int ItemControlWidth;
        internal static int ItemControlHeigth;

        internal SortBy SortBy = SortBy.Random;
        internal bool SortAscending = true;
        
        internal delegate void PhotoMouseDownHandler(Photo photo, MouseEventArgs e);
        internal event PhotoMouseDownHandler PhotoMouseDown;
        internal delegate void PhotoMouseUpHandler(Photo photo, MouseEventArgs e);
        internal event PhotoMouseUpHandler PhotoMouseUp;
        internal delegate void PhotoMouseMoveHandler(Photo photo, MouseEventArgs e);
        internal event PhotoMouseMoveHandler PhotoMouseMove;

        private List<Photo> m_Photos = new List<Photo>();
        private List<Photo> m_SelectedItems = new List<Photo>();
        private static readonly object m_Locker = new object();
        private List<MyListViewItemControl> m_ItemControls = new List<MyListViewItemControl>();
        private List<MyListViewGroupBar> m_GroupBarControls = new List<MyListViewGroupBar>();
        private Dictionary<Guid, Image> m_Thumbnails = new Dictionary<Guid, Image>();
        private VScrollBar m_VScrollBar = new VScrollBar();
        private bool m_Updating = false;
        private int m_VisibleRows = 0;
        private int m_VisibleColumns = 0;
        private int m_FirstVisibleIndex = 0;

        internal MyListView()
        {
            InitializeComponent();            
        }

        private void InitializeComponent()
        {
            ItemControlWidth = Thumbnails.ThumbnailSize;
            ItemControlHeigth = ItemControlWidth + 35;

            this.BorderStyle = BorderStyle.Fixed3D;
            this.BackColor = Color.White;
            //this.SetStyle(ControlStyles.Selectable, true);
            //this.TabStop = true;

            m_VScrollBar.Dock = DockStyle.Right;
            m_VScrollBar.Maximum = 0;
            m_VScrollBar.Value = 0;

            m_VScrollBar.ValueChanged += new EventHandler(m_VScrollBar_ValueChanged);

            this.Resize += new EventHandler(MyListView_Resize);
            this.MouseMove += new MouseEventHandler(MyListView_MouseMove);

            this.Controls.Add(m_VScrollBar);

            Worklist.OnThumbnailLoaded += new Worklist.ThumbnailLoadedHandler(Worklist_OnThumbnailLoaded);
        }

        internal bool Scroll(bool down, bool largeStep)
        {
            int value = m_VScrollBar.Value + (largeStep ? m_VScrollBar.LargeChange : m_VScrollBar.SmallChange) * (down ? 1 : -1);

            if (value < 0)
                value = 0;
            else if (value > m_VScrollBar.Maximum)
                value = m_VScrollBar.Maximum;

            if (value == m_VScrollBar.Value)
                return false;

            m_VScrollBar.Value = value;

            return true;
        }

        void MyListView_MouseMove(object sender, MouseEventArgs e)
        {
            //System.Diagnostics.Trace.WriteLine(e.Location.ToString() + " " + e.Delta.ToString());
        }

        void Worklist_OnThumbnailLoaded(Guid photoId, Image thumbnail)
        {
            if (!m_Thumbnails.ContainsKey(photoId))
                m_Thumbnails.Add(photoId, thumbnail);

            lock (m_Locker)
            {
                for (int i=0; i<m_ItemControls.Count; i++)
                    if (m_ItemControls[i].Photo != null && m_ItemControls[i].Photo.Id == photoId)
                        m_ItemControls[i].Image = thumbnail;
            }
        }

        void m_VScrollBar_ValueChanged(object sender, EventArgs e)
        {            
            RefreshOrWhatever();
        }

        void MyListView_Resize(object sender, EventArgs e)
        {
            m_VisibleColumns = (this.ClientRectangle.Width - m_VScrollBar.Width - 10) / (ItemControlWidth + 10);
            if (m_VisibleColumns == 0)
                m_VisibleColumns = 1;
            m_VisibleRows = (this.ClientRectangle.Height - 10) / (ItemControlHeigth + 10);
            
            RefreshOrWhatever();
        }

        internal bool KeyPreview(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    MoveSelection(1, 0);
                    break;
                case Keys.Left:
                    MoveSelection(-1, 0);
                    break;
                case Keys.Down:
                    MoveSelection(0, 1);
                    break;
                case Keys.Up:
                    MoveSelection(0, -1);
                    break;
                case Keys.A:
                    if (e.Control)
                        SelectAll();
                    break;
            }
            return false;
        }

        internal void PageDown()
        {
            if (m_VScrollBar.Value + m_VisibleRows < m_VScrollBar.Maximum)
            {
                //MoveSelection(0, m_VisibleRows);
                m_VScrollBar.Value += m_VisibleRows;
            }            
        }

        internal void PageUp()
        {
            int x = m_VScrollBar.Value - m_VisibleRows;
            if (x < 0)
                x = 0;

            //MoveSelection(0, x - m_VScrollBar.Value);
            m_VScrollBar.Value = x;
        }

        private void SelectAll()
        {
            m_SelectedItems.Clear();
            foreach (Photo photo in m_Photos)
                m_SelectedItems.Add(photo);
            RefreshOrWhatever();
        }

        public void EnsureSelectionVisible()
        {
            if (m_SelectedItems.Count != 1)
                return;

            int x = m_Photos.IndexOf(m_SelectedItems[0]);

            if (x < m_FirstVisibleIndex)
            {
                m_VScrollBar.Value = x / m_VisibleColumns;
            }
            else 
                if (x >= m_FirstVisibleIndex + m_VisibleColumns * m_VisibleRows)
                {
                    m_VScrollBar.Value = x / m_VisibleColumns - m_VisibleRows + 1;
                }                        
        }

        private void MoveSelection(int columns, int rows)
        {            
            if (m_SelectedItems.Count == 0)
                return;

            int newIndex = SelectedIndices[0] + rows * m_VisibleColumns + columns;

            if (newIndex >= m_Photos.Count || newIndex < 0)
                return;

            m_SelectedItems.Clear();
            m_SelectedItems.Add(m_Photos[newIndex]);

            RefreshOrWhatever();
            EnsureSelectionVisible();
        }

        internal void Clear()
        {
            m_Photos.Clear();
            m_SelectedItems.Clear();

            m_VScrollBar.Maximum = 0;
            m_VScrollBar.Value = 0;
        }

        internal void BeginUpdate()
        {
            m_Updating = true;
        }

        internal void EndUpdate()
        {
            EndUpdate(true);
        }

        internal void EndUpdate(bool sorting)
        {
            m_Updating = false;
            if (sorting)
                Sort();
            RefreshOrWhatever();
            SelectedIndex = 0;
        }

        internal void Sort()
        {
            SortedDictionary<string, Photo> sorter = new SortedDictionary<string, Photo>();

            Random random = new Random();
            foreach (Photo photo in m_Photos)
                switch (this.SortBy)
                {
                    case SortBy.Filename:
                        sorter.Add(photo.Filename + "|" + photo.Id.ToString(), photo);
                        break;
                    case SortBy.Folder:
                        sorter.Add(photo.Path + "|" + photo.Filename + "|" + photo.Id.ToString(), photo);
                        break;
                    case SortBy.Filesize:
                        sorter.Add(photo.FileSize.ToString().PadLeft(10, '0') + "|" + photo.Id.ToString(), photo);
                        break;
                    case SortBy.ImportDate:
                        sorter.Add(photo.ImportDate.ToString("yyyyMMddHHmmss") + "|" + photo.Id.ToString(), photo);
                        break;
                    case SortBy.Resolution:
                        sorter.Add((photo.Width * photo.Height).ToString("0000000000") + "|" + photo.Id.ToString(), photo);
                        break;
                    case SortBy.Width:
                        sorter.Add(photo.Width.ToString("00000") + "|" + photo.Id.ToString(), photo);
                        break;
                    case SortBy.Height:
                        sorter.Add(photo.Height.ToString("00000") + "|" + photo.Id.ToString(), photo);
                        break;
                    case SortBy.Random:
                        string test = random.Next(0, 1000000).ToString("00000000");
                        sorter.Add(test + "|" + photo.Id.ToString(), photo);
                        break;
                }                

            m_Photos.Clear();

            if (this.SortAscending)               
                foreach (Photo photo in sorter.Values)
                    m_Photos.Add(photo);                
            else
                foreach (Photo photo in sorter.Values.Reverse())
                    m_Photos.Add(photo);                
        }

        internal void RefreshOrWhatever()
        {
            if (m_Updating)
                return;

            CalculateScrollBarMaxValue();

            Photo photo = null, prevPhoto = null;
            MyListViewItemControl ctl = null;

            int column = 0;
            int row = 0;
            int usedItems = 0;
            int verticalOffset = 10;

            MyListViewGroupBar groupBar = null;
            while (m_GroupBarControls.Count > 0)
            {
                groupBar = m_GroupBarControls[m_GroupBarControls.Count - 1];
                this.Controls.Remove(groupBar);
                m_GroupBarControls.Remove(groupBar);
            }
            groupBar = null;

            m_FirstVisibleIndex = m_VScrollBar.Value * m_VisibleColumns;
            for (int i = m_FirstVisibleIndex; i < m_Photos.Count; i++)
            {
                if (usedItems < m_ItemControls.Count)
                {
                    ctl = m_ItemControls[usedItems];
                }
                else
                {
                    ctl = new MyListViewItemControl();
                    //ctl.OnMouseDown += new MyListViewItemControl.MouseDownHandler(ctl_OnMouseDown);
                    //ctl.OnMouseUp += new MyListViewItemControl.MouseUpHandler(ctl_OnMouseUp);
                    ctl.OnMouseEvent += new MyListViewItemControl.MouseEventsHandler(ctl_OnMouseEvent);
                    m_ItemControls.Add(ctl);
                    this.Controls.Add(ctl);
                }

                usedItems++;

                photo = m_Photos[i];
                prevPhoto = (i == 0 ? null : m_Photos[i - 1]);

                groupBar = GetGroupBar(prevPhoto, photo);
                if (groupBar != null)
                {
                    if (i != m_FirstVisibleIndex)
                        verticalOffset += 30;

                    if (column > 0)
                    {
                        row++;
                        column = 0;
                    }

                    groupBar.Top = verticalOffset + row * (ItemControlHeigth + 10);
                    groupBar.Width = this.ClientRectangle.Width;
                    m_GroupBarControls.Add(groupBar);
                    this.Controls.Add(groupBar);
                    verticalOffset += groupBar.Height + 5;
                }

                ctl.Photo = photo;
                ctl.Selected = m_SelectedItems.Contains(photo);

                if (m_Thumbnails.ContainsKey(photo.Id))
                    ctl.Image = m_Thumbnails[photo.Id];
                else
                    ctl.Image = null;

                ctl.Left = 10 + column * (ItemControlWidth + 10);
                ctl.Top = verticalOffset + row * (ItemControlHeigth + 10);

                column++;
                if (column >= m_VisibleColumns)
                {
                    row++;
                    if (row > m_VisibleRows)
                        break;

                    column = 0;
                }
            }

            lock (m_Locker)
            {
                for (int i = m_ItemControls.Count - 1; i >= usedItems; i--)
                {
                    ctl = m_ItemControls[i];
                    //ctl.OnMouseDown -= new MyListViewItemControl.MouseDownHandler(ctl_OnMouseDown);
                    //ctl.OnMouseUp -= new MyListViewItemControl.MouseUpHandler(ctl_OnMouseUp);
                    ctl.OnMouseEvent -= new MyListViewItemControl.MouseEventsHandler(ctl_OnMouseEvent);
                    this.Controls.Remove(ctl);
                    ctl.Dispose();
                    m_ItemControls.Remove(ctl);
                }
            }

            RequestThumbnails();
        }

        void ctl_OnMouseEvent(string eventType, MyListViewItemControl sender, MouseEventArgs e)
        {
            MouseEventArgs mea = new MouseEventArgs(e.Button, e.Clicks, e.X + sender.Left, e.Y + sender.Top, e.Delta);

            switch (eventType)
            {
                case "move":
                    if (PhotoMouseMove != null)
                        PhotoMouseMove(sender.Photo, mea);
                    break;
                case "up":
                    if (PhotoMouseUp != null)
                        PhotoMouseUp(sender.Photo, mea);
                    break;
                case "down":
                    ctl_OnMouseDown(sender, mea);                    
                    break;
            }
        }

        private MyListViewGroupBar GetGroupBar(Photo prevPhoto, Photo photo)
        {
            switch (this.SortBy)
            { 
                case SortBy.Folder:
                    if (prevPhoto == null || photo.Path != prevPhoto.Path)
                        return new MyListViewGroupBar(photo.Path);
                    break;
                case SortBy.ImportDate:
                    if (prevPhoto == null || photo.ImportDate != prevPhoto.ImportDate)
                        return new MyListViewGroupBar(photo.ImportDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    break;
            }

            return null;
        }

        private void RequestThumbnails()
        {
            if (m_Photos.Count == 0)
                return;
            
            List<Photo> worklist = new List<Photo>();
            Dictionary<Guid, Image> thumbnails = new Dictionary<Guid,Image>();
            Dictionary<Guid, Image> dummy = null;
            int visibleItemCount = m_VisibleColumns * (m_VisibleRows + 1); // check this?

            AddThumbnailRange(ref thumbnails, ref worklist, m_FirstVisibleIndex, visibleItemCount);
            AddThumbnailRange(ref thumbnails, ref worklist, m_FirstVisibleIndex - visibleItemCount, visibleItemCount);
            AddThumbnailRange(ref thumbnails, ref worklist, m_FirstVisibleIndex + visibleItemCount, visibleItemCount);

            dummy = m_Thumbnails;
            m_Thumbnails = thumbnails;
// todo: System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
            foreach (Image image in dummy.Values)
                if (!thumbnails.ContainsValue(image))
                    try
                    {
                        image.Dispose();
                    }
                    catch
                    {
                    }
            dummy.Clear();

            Worklist.SetWorkList(worklist);
            Loader.LoadThumbnails();
        }

        private void AddThumbnailRange(ref Dictionary<Guid, Image> thumbnails, ref List<Photo> worklist, int firstIndex, int count)
        {
            if (firstIndex < 0)
            {
                count += firstIndex;
                firstIndex = 0;
            }

            for (int i = firstIndex; i < firstIndex + count; i++)
            {
                if (i >= m_Photos.Count)
                    return;

                if (m_Thumbnails.ContainsKey(m_Photos[i].Id))
                    thumbnails.Add(m_Photos[i].Id, m_Thumbnails[m_Photos[i].Id]);
                else
                    worklist.Add(m_Photos[i]);
            }
        }

        void ctl_OnMouseDown(MyListViewItemControl sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (ModifierKeys == Keys.Control)
                {
                    if (m_SelectedItems.Contains(sender.Photo))
                        m_SelectedItems.Remove(sender.Photo);
                    else
                        m_SelectedItems.Add(sender.Photo);

                    RefreshOrWhatever();
                    return;
                }
                
                if (ModifierKeys == Keys.Shift && m_SelectedItems.Count > 0)
                {
                    int begin = -1, end = -1;
                    Photo photo = m_SelectedItems[0];
                    m_SelectedItems.Clear();

                    for (int i = 0; i < m_Photos.Count; i++)
                    {
                        if (m_Photos[i] == photo)
                            begin = i;
                        if (m_Photos[i] == sender.Photo)
                            end = i;
                    }

                    if (begin < end)
                        for (int i = begin; i <= end; i++)
                            m_SelectedItems.Add(m_Photos[i]);
                    else
                        for (int i = begin; i >= end; i--)
                            m_SelectedItems.Add(m_Photos[i]);

                    RefreshOrWhatever();
                    return;
                }

                m_SelectedItems.Clear();
                m_SelectedItems.Add(sender.Photo);

                if (ModifierKeys != Keys.Shift && ModifierKeys != Keys.Control)
                    EnsureSelectionVisible();
                
                RefreshOrWhatever();
            }            

            if (PhotoMouseDown != null)
                PhotoMouseDown(sender.Photo, e);
        }

        //void ctl_OnMouseUp(MyListViewItemControl sender, MouseEventArgs e)
        //{
        //    if (PhotoMouseUp != null)
        //        PhotoMouseUp(sender.Photo, e);
        //}

        private void CalculateScrollBarMaxValue()
        {
            int maximum = (int)Math.Ceiling((decimal)m_Photos.Count / m_VisibleColumns); // -m_VisibleRows + 1;

            m_VScrollBar.Maximum = (maximum > 0 ? maximum : 0);
            m_VScrollBar.SmallChange = 1;
            m_VScrollBar.LargeChange = m_VisibleRows;

            //System.Diagnostics.Trace.WriteLine("min: " + m_VScrollBar.Minimum.ToString() + " max: " + m_VScrollBar.Maximum.ToString() + " value: " + m_VScrollBar.Value.ToString());           
        }

        internal void GotSomeImages(Dictionary<string, Image> thumbnails)
        {
            foreach (KeyValuePair<string, Image> kvp in thumbnails)
                if (!m_Thumbnails.ContainsKey(new Guid(kvp.Key)))
                    m_Thumbnails.Add(new Guid(kvp.Key), kvp.Value);
        }
    }
}