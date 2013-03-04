using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Photo.org
{
    internal class MyListViewGroupBar : UserControl
    {
        private Label m_Label = new Label();

        internal MyListViewGroupBar()
        {
            this.BackColor = Color.LightBlue;
            this.Height = 20;

            this.Controls.Add(m_Label);

            this.Resize += new EventHandler(MyListViewGroupBar_Resize);
        }

        void MyListViewGroupBar_Resize(object sender, EventArgs e)
        {
            m_Label.Width = this.ClientRectangle.Width;
        }

        internal MyListViewGroupBar(string text) : this()
        {
            m_Label.Text = text;
        }
    }
}
