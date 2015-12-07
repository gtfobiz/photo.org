using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualBasic.FileIO;

namespace Photo.org
{
    internal static class Common
    {
        private static MainForm m_MainForm = null;
        private static SplitContainer m_SplitContainer = new SplitContainer();
        private static bool m_LayoutVisited = false;
        public static string CommandLineDatabaseFilename = "";
        private static int m_StoredSplitterDistance = 0;
        private static FormWindowState m_StoredFormWindowState = FormWindowState.Normal;

        internal static void Initialize(MainForm mainForm)
        {
            Settings.Load();

            m_MainForm = mainForm;
            m_MainForm.KeyPreview = true;
            m_MainForm.StartPosition = FormStartPosition.Manual;
            m_MainForm.MinimumSize = new Size(800, 600);
            //mainForm.WindowState = (FormWindowState)StrToInt(Setting.MainWindowState);
            mainForm.Left = StrToInt(Settings.Get(Setting.MainWindowLeft));
            mainForm.Top = StrToInt(Settings.Get(Setting.MainWindowTop));
            mainForm.Width = StrToInt(Settings.Get(Setting.MainWindowWidth));
            mainForm.Height = StrToInt(Settings.Get(Setting.MainWindowHeight));

            Multilingual.Load();                       

            m_SplitContainer.Dock = DockStyle.Fill;
            m_SplitContainer.GotFocus += new EventHandler(m_SplitContainer_GotFocus);
            m_MainForm.Controls.Add(m_SplitContainer);

            m_MainForm.KeyDown += new KeyEventHandler(MainForm_KeyDown);
            m_MainForm.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
            m_MainForm.Layout += m_MainForm_Layout;

            Status.Initialize(mainForm);
            Categories.Initialize(m_SplitContainer.Panel1.Controls);
            Thumbnails.Initialize(m_SplitContainer.Panel2.Controls);                
            Menus.Initialize(m_MainForm);

            Application.DoEvents();

            Database.OpenDefaultDatabase();
            Categories.Populate();

            //new testbed().Show();

            //Worklist.StartThread();
        }

        static void m_MainForm_Layout(object sender, LayoutEventArgs e)
        {
            if (m_LayoutVisited)
                return;

            m_SplitContainer.SplitterDistance = StrToInt(Settings.Get(Setting.SplitterDistance));
            m_LayoutVisited = true;
        }

        internal static int StrToInt(string value)
        {
            try
            {
                return int.Parse(value);
            }
            catch
            {
                return 0;
            }
        }

        internal static bool IsCtrlPressed()
        {
            return m_MainForm.IsCtrlPressed();
        }

        internal static bool IsShiftPressed()
        {
            return m_MainForm.IsShiftPressed();
        }

        internal static bool IsAltPressed()
        {
            return m_MainForm.IsAltPressed();
        }

        static void m_SplitContainer_GotFocus(object sender, EventArgs e)
        {
            Categories.TakeFocus();
        }

        internal static void SetFormCaption(string caption)
        {
            m_MainForm.Text = caption;
        }

        static void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Set(Setting.MainWindowLeft, m_MainForm.Left.ToString());
            Settings.Set(Setting.MainWindowTop, m_MainForm.Top.ToString());
            Settings.Set(Setting.MainWindowWidth, m_MainForm.Width.ToString());
            Settings.Set(Setting.MainWindowHeight, m_MainForm.Height.ToString());
            //Settings.Set(Setting.MainWindowState, ((int)m_MainForm.WindowState).ToString());
            Settings.Set(Setting.SplitterDistance, m_SplitContainer.SplitterDistance.ToString());            

            Dispose();
        }

        internal static void SetFullscreen(bool state)
        {
            if (state)
            {
                Menus.SetVisible(false);
                Status.SetVisible(false);

                m_StoredFormWindowState = m_MainForm.WindowState;
                m_StoredSplitterDistance = m_SplitContainer.SplitterDistance;

                m_MainForm.FormBorderStyle = FormBorderStyle.None;
                m_MainForm.WindowState = FormWindowState.Maximized;
                m_SplitContainer.SplitterDistance = 0;
                m_SplitContainer.Panel1Collapsed = true;
            }
            else
            {
                Menus.SetVisible(true);
                Status.SetVisible(true);

                m_MainForm.FormBorderStyle = FormBorderStyle.Sizable;
                m_MainForm.WindowState = m_StoredFormWindowState;                
                m_SplitContainer.Panel1Collapsed = false;
                m_SplitContainer.SplitterDistance = m_StoredSplitterDistance;
            }
        }

        // temp test
        static public void ShowAllExif(string filename)
        {
            System.Drawing.Image image = System.Drawing.Image.FromFile(filename);

            foreach (System.Drawing.Imaging.PropertyItem pi in image.PropertyItems)
            {
                System.Diagnostics.Trace.WriteLine(pi.Id.ToString() + "\t" + System.Text.Encoding.Default.GetString(pi.Value));
            }
        }

        static public void SendToRecycleBin(string filename)
        {
            FileSystem.DeleteFile(filename, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }

        internal static string GetFileSizeString(long fileSize)
        {
            if (fileSize < 1024)
                return fileSize.ToString() + "B";
            if (fileSize < 1024 * 1024)
                return ((decimal)fileSize / 1024).ToString("0") + "KB";
            return ((decimal)fileSize / 1024 / 1024).ToString("0.0") + "MB";
        }

        static void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            bool suppressKeyPress = false;

            switch (Status.ActiveComponent)
            { 
                case Component.Photos:
                    suppressKeyPress = Thumbnails.KeyPreview(e);
                    break;
                case Component.Viewer:
                    suppressKeyPress = Viewer.KeyPreview(e);
                    break;
            }

            if (!suppressKeyPress)
            {
                HandleKeyDown(e);
            }

            e.SuppressKeyPress = suppressKeyPress;
        }

        private static void HandleKeyDown(KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Space)
            //{
            //    using (CategoryForm f = new CategoryForm())
            //    {
            //        f.ShowDialog();
            //    }
            //}
        }

        internal static void OnDatabaseChange()       
        {
            Thumbnails.ClearPhotos();
            Thumbnails.Show();
            Categories.ClearCategories();
            Categories.Populate();
        }

#region hashing

        internal static string GetMD5HashForImage(System.Drawing.Image image)
        {
            using (MemoryStream strm = new MemoryStream())
            {
                image.Save(strm, System.Drawing.Imaging.ImageFormat.Png);

                byte[] imgBytes = strm.ToArray();
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] hash = md5.ComputeHash(imgBytes);
                string imageMD5 = BitConverter.ToString(hash).Replace("-", "").ToLower();
                strm.Dispose();

                return imageMD5;

                //MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                //string encoded = BitConverter.ToString(md5.ComputeHash(strm)).Replace("-", null).ToLower();                
                //strm.Close();
                //md5.Clear();
                //return encoded;
            }

            //System.Drawing.ImageConverter ic = new System.Drawing.ImageConverter();
            //byte[] bs = new byte[1];
            //try
            //{
            //    bs = (byte[])ic.ConvertTo(image, bs.GetType());
            //}
            //catch 
            //{
            //    return null;
            //}

            //System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //bs = x.ComputeHash(bs);
            //System.Text.StringBuilder s = new System.Text.StringBuilder();
            //foreach (byte b in bs)
            //{
            //    s.Append(b.ToString("x2").ToLower());
            //}
            //return s.ToString();
        }

        internal static string xxGetMD5Hash(string input)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();
        }

#endregion        

        internal static string InputBox(string prompt)
        {
            using (InputBoxForm f = new InputBoxForm())
            {
                f.Prompt = prompt;
                f.ShowDialog();
                if (f.DialogResult == DialogResult.Cancel)
                    return null;

                return f.InputText;
            }            
        }

        internal static void Dispose()
        {
            try
            {
                Thumbnails.ClearPhotos(); // Clears worklist of the background thread
                //Worklist.StopThread();
                Settings.Save();
                Multilingual.Save();
            }
            catch
            { 
            }
            Database.Dispose();
        }

        internal static void Exit()
        {
            Application.Exit();
        }

        internal static void MouseWheelOutsideTreeView(bool mouseWheelForward)
        {
            switch (Status.ActiveComponent)
            {
                case Component.Photos:
                    Thumbnails.MouseWheelOutsideTreeView(mouseWheelForward);
                    break;
                case Component.Viewer:
                    Viewer.MouseWheelOutsideTreeView(mouseWheelForward);
                    break;
            }
        }

        internal static void ParseCommandLine(string[] args)
        {
            bool nextArgIsDatabaseFilename = false;

            foreach (string arg in args)
            {
                if (nextArgIsDatabaseFilename)
                {
                    CommandLineDatabaseFilename = arg;
                    nextArgIsDatabaseFilename = false;
                    continue;
                }

                switch (arg.ToLower())
                {
                    case "-r":
                        Status.ReadOnly = true;
                        break;
                    case "-h":
                        Status.ShowHiddenPhotos = true;
                        break;
                    case "-x":
                        Status.ShowHiddenCategories = true;
                        break;
                    case "-d":
                        nextArgIsDatabaseFilename = true;
                        break;
                }
            }                   
        }
    }
}
