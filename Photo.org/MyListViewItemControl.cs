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
        internal delegate void MouseEventsHandler(string eventType, MyListViewItemControl sender, MouseEventArgs e);
        internal event MouseEventsHandler OnMouseEvent;

        private Photo m_Photo = null;        
        private bool m_Selected = false;
        private string m_Text = "";
        private Label m_Label = new Label();
        private PictureBox m_PictureBox = new PictureBox();
        private ToolTip m_ToolTip = new ToolTip();

        internal MyListViewItemControl()
        {            
            m_PictureBox.Location = new Point(0, 0);
            m_PictureBox.Size = new Size(MyListView.ItemControlWidth, MyListView.ItemControlWidth);
            m_PictureBox.BackColor = SystemColors.Control;
            m_PictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            m_PictureBox.MouseDown += new MouseEventHandler(OnMouseDown);
            m_PictureBox.MouseUp += new MouseEventHandler(OnMouseUp);
            m_PictureBox.MouseMove += new MouseEventHandler(OnMouseMove);
            this.Controls.Add(m_PictureBox);

            m_Label.Location = new Point(0, MyListView.ItemControlHeigth - 30);
            m_Label.AutoSize = false;
            m_Label.Size = new Size(MyListView.ItemControlWidth, 50);
            m_Label.BackColor = SystemColors.Control;
            m_Label.TextAlign = ContentAlignment.TopCenter;
            m_Label.MouseDown += new MouseEventHandler(OnMouseDown);
            m_Label.MouseUp += new MouseEventHandler(OnMouseUp);
            m_Label.MouseMove += new MouseEventHandler(OnMouseMove);
            this.Controls.Add(m_Label);

            m_ToolTip.InitialDelay = 200;

            this.Size = new Size(MyListView.ItemControlWidth, MyListView.ItemControlHeigth);
            this.MouseDown += new MouseEventHandler(OnMouseDown);
            this.MouseUp += new MouseEventHandler(OnMouseUp);
            this.MouseMove += new MouseEventHandler(OnMouseMove);
        }

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

                string toolTipText = "";

                foreach (Guid guid in m_Photo.Categories)
                    if (guid != Guids.Hidden)
                    {
                        toolTipText += (toolTipText == "" ? "" : ", ") + Categories.GetCategoryByGuid(guid).Name;
                    }

                toolTipText = m_Photo.Filename + "  (" + Common.GetFileSizeString(m_Photo.FileSize) + ")\n\n" + toolTipText;

                this.SetTooltipText(toolTipText);
            }
        }

        private void SetTooltipText(string text)
        {            
            m_ToolTip.SetToolTip(this, text);
            foreach (Control control in this.Controls)
                m_ToolTip.SetToolTip(control, text);
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
