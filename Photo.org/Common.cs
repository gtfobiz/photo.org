﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;

namespace Photo.org
{
    internal static class Common
    {
        private static MainForm m_MainForm = null;
        private static SplitContainer m_SplitContainer = new SplitContainer();        

        internal static void Initialize(MainForm mainForm)
        {
            Settings.Load();
            Multilingual.Load();

            m_MainForm = mainForm;
            m_MainForm.KeyPreview = true;

            m_SplitContainer.Dock = DockStyle.Fill;
            m_SplitContainer.GotFocus += new EventHandler(m_SplitContainer_GotFocus);
            m_MainForm.Controls.Add(m_SplitContainer);

            m_MainForm.KeyDown += new KeyEventHandler(MainForm_KeyDown);
            m_MainForm.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);

            Status.Initialize(mainForm);
            Categories.Initialize(m_SplitContainer.Panel1.Controls);
            Thumbnails.Initialize(m_SplitContainer.Panel2.Controls);                
            Menus.Initialize(m_MainForm);

            Application.DoEvents();

            Database.OpenDefaultDatabase();
            Categories.Populate();

            //Worklist.StartThread();
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

        static void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Dispose();
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
                image.Save(strm, System.Drawing.Imaging.ImageFormat.Bmp);
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                string encoded = BitConverter.ToString(md5.ComputeHash(strm)).Replace("-", null).ToLower();                
                strm.Close();
                md5.Clear();
                return encoded;
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

        internal static string GetMD5Hash(string input)
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
            if (args.Contains("-r") || args.Contains("-R"))
                Status.ReadOnly = true;

            if (args.Contains("-h") || args.Contains("-H"))
                Status.ShowHiddenPhotos = true;
        }
    }
}