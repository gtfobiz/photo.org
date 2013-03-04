using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Photo.org
{
    internal class MyListViewItemControl : UserControl
    {
        //internal delegate void MouseDownHandler(MyListViewItemControl sender, MouseEventArgs e);
        //internal new event MouseDownHandler OnMouseDown;
        //internal delegate void MouseUpHandler(MyListViewItemControl sender, MouseEventArgs e);
        //internal new event MouseUpHandler OnMouseUp;
        internal delegate void MouseEventsHandler(string eventType, MyListViewItemControl sender, MouseEventArgs e);
        internal new event MouseEventsHandler OnMouseEvent;

        private Photo m_Photo = null;

        internal Photo Photo
        {
            get
            {
                return m_Photo;
            }
            set
            {
                m_Photo = value;

                this.Text = m_Photo.Filename;
            }
        }

        private bool m_Selected = false;
        private string m_Text = "";
        private Label m_Label = new Label();
        private PictureBox m_PictureBox = new PictureBox();

        internal MyListViewItemControl()
        {
            m_PictureBox.Location = new Point(0, 0);
            m_PictureBox.Size = new Size(MyListView.ItemControlWidth, MyListView.ItemControlWidth);
            m_PictureBox.BackColor = SystemColors.Control;
            m_PictureBox.MouseDown += new MouseEventHandler(OnMouseDown);
            m_PictureBox.MouseUp += new MouseEventHandler(OnMouseUp);
            m_PictureBox.MouseMove += new MouseEventHandler(OnMouseMove);
            this.Controls.Add(m_PictureBox);

            m_Label.Location = new Point(0, 105);
            m_Label.AutoSize = false;
            m_Label.Size = new Size(MyListView.ItemControlWidth, 50);
            m_Label.BackColor = SystemColors.Control;
            m_Label.TextAlign = ContentAlignment.TopCenter;
            m_Label.MouseDown += new MouseEventHandler(OnMouseDown);
            m_Label.MouseUp += new MouseEventHandler(OnMouseUp);
            m_Label.MouseMove += new MouseEventHandler(OnMouseMove);
            this.Controls.Add(m_Label);

            this.Size = new Size(MyListView.ItemControlWidth, MyListView.ItemControlHeigth);
            this.MouseDown += new MouseEventHandler(OnMouseDown);
            this.MouseUp += new MouseEventHandler(OnMouseUp);
            this.MouseMove += new MouseEventHandler(OnMouseMove);
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            HandleMouseEvent("move", e);
        }

        void OnMouseUp(object sender, MouseEventArgs e)
        {
            HandleMouseEvent("up", e);
        }

        void OnMouseDown(object sender, MouseEventArgs e)
        {
            HandleMouseEvent("down", e);
        }

        private void HandleMouseEvent(string eventType, MouseEventArgs e)
        {
            if (OnMouseEvent != null)
                OnMouseEvent(eventType, this, e);
        }

        //void m_Label_MouseDown(object sender, MouseEventArgs e)
        //{
        //    HandleMouseDown(e);
        //}

        //void m_PictureBox_MouseDown(object sender, MouseEventArgs e)
        //{
        //    HandleMouseDown(e);
        //}

        //void MyListViewItemControl_MouseDown(object sender, MouseEventArgs e)
        //{
        //    HandleMouseDown(e);
        //}

        //private void HandleMouseDown(MouseEventArgs e)
        //{
        //    if (OnMouseDown != null)
        //        OnMouseDown(this, e);
        //}

        //void m_Label_MouseUp(object sender, MouseEventArgs e)
        //{
        //    HandleMouseUp(e);
        //}

        //void m_PictureBox_MouseUp(object sender, MouseEventArgs e)
        //{
        //    HandleMouseUp(e);
        //}

        //void MyListViewItemControl_MouseUp(object sender, MouseEventArgs e)
        //{
        //    HandleMouseUp(e);
        //}

        //private void HandleMouseUp(MouseEventArgs e)
        //{
        //    if (OnMouseUp != null)
        //        OnMouseUp(this, e);
        //}

        internal Image Image
        {
            get { return m_PictureBox.Image; }
            set
            {
                try
                {
                    m_PictureBox.Image = value; // Object is currently in use elsewhere. -> TODO
                }
                catch
                {
                }
            }
        }

        internal new string Text
        {
            get { return m_Text; }
            set { m_Text = value; m_Label.Text = m_Text; }
        }

        internal bool Selected
        {
            get { return m_Selected; }
            set
            {
                m_Selected = value;
                UpdateColors();
            } 
        }

        private void UpdateColors()
        {
            if (m_Photo == null || !m_Photo.Categories.Contains(Guids.Hidden))
                m_Label.BackColor = (m_Selected ? Color.LightGreen : SystemColors.Control);
            else
                m_Label.BackColor = (m_Selected ? Color.LightGreen : Color.Pink);
        }
    }
}
