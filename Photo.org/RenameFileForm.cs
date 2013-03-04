using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Photo.org
{
    public partial class RenameFileForm : Form
    {
        private FileInfo m_FI = null;
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
                m_FI = new FileInfo(m_Photo.FilenameWithPath);

                f_Filename.Text = m_FI.Name;
                f_Filename.SelectionLength = m_FI.Name.Length - m_FI.Extension.Length;
            }
        }

        public RenameFileForm()
        {
            InitializeComponent();
        }

        private void cmdOk_Click(object sender, EventArgs e)
        {
            string filename = f_Filename.Text;            

            if (filename == "")
                return;            

            try
            {
                Status.Busy = true;

                File.Move(m_Photo.FilenameWithPath, m_Photo.Path + @"\" + filename);

                m_Photo.Filename = filename;
                Database.UpdatePhotoLocation(m_Photo.Id, m_Photo.Path, m_Photo.Filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
            finally
            {
                Status.Busy = false;
            }            

            this.Close();
        }
    }
}
