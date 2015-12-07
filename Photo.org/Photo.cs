using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Photo.org
{
    internal class Photo
    {
        private Guid m_Id;
        private string m_Hash = "";
        private string m_Filename = "";
        private string m_Path = "";
        private long m_FileSize = 0;
        private long m_Width = 0;        
        private long m_Height = 0;        
        private DateTime m_ImportDate = DateTime.MinValue;
        private bool m_IsVideo = false;

        private List<Guid> m_Categories = null;
        private List<Guid> m_AutoCategories = null;

        public List<Guid> AutoCategories
        {
            get { return m_AutoCategories; }
            set { m_AutoCategories = value; }
        }

        private bool m_Busy = false;
//        private string file;

        public Image LoadImage()
        {
            try
            {
                MemoryStream ms = new MemoryStream(File.ReadAllBytes(this.FilenameWithPath));
                return Image.FromStream(ms);
            }
            catch
            {
                return null;
            }
        }

        public List<Guid> Categories
        {
            get { return m_Categories; }
            set { m_Categories = value; }
        }
        
        internal Photo()
        {
        }

        internal Image CreateThumbnail()
        {
            return CreateThumbnail(null);
        }

        public Image CreateThumbnailFromVideo()
        {
            string thumbnail = Database.ThumbnailPath + @"\" + m_Id.ToString();
            string size = Thumbnails.ThumbnailSize.ToString() + "x" + Thumbnails.ThumbnailSize.ToString();
            string cmd = "ffmpeg  -itsoffset -1  -i " + '"' + this.FilenameWithPath + '"' + " -vcodec mjpeg -vframes 1 -an -f rawvideo -s " + size + " " + '"' + thumbnail + '"';

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C " + cmd
            };

            System.Diagnostics.Process process = new System.Diagnostics.Process
            {
                StartInfo = startInfo
            };

            process.Start();
            process.WaitForExit(5000);

            var ms = new MemoryStream(File.ReadAllBytes(thumbnail));
            return Image.FromStream(ms);
        }        

        //public static Bitmap GetThumbnail(string video, string thumbnail)
        //{
        //    var cmd = "ffmpeg  -itsoffset -1  -i " + '"' + video + '"' + " -vcodec mjpeg -vframes 1 -an -f rawvideo -s 320x240 " + '"' + thumbnail + '"';

        //    var startInfo = new ProcessStartInfo
        //    {
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //        FileName = "cmd.exe",
        //        Arguments = "/C " + cmd
        //    };

        //    var process = new Process
        //    {
        //        StartInfo = startInfo
        //    };

        //    process.Start();
        //    process.WaitForExit(5000);

        //    return LoadImage(thumbnail);
        //}

        //static Bitmap LoadImage(string path)
        //{
        //    var ms = new MemoryStream(File.ReadAllBytes(path));
        //    return (Bitmap)Image.FromStream(ms);
        //}

        internal Image CreateThumbnail(Image image)
        {
            if (m_Busy)
                return null;

            m_Busy = true;

            if (image == null)
            {
                if (!this.Exists())
                    return null;

                image = Image.FromFile(this.FilenameWithPath);
                //image = LoadImage();
            }

            Image thumbnail = ThumbnailFromImage(image, Thumbnails.ThumbnailSize);
            if (thumbnail == null)
                return null;

            //new Bitmap(thumbnail).Save(Database.ThumbnailPath + @"\" + m_Id.ToString(), ImageFormat.Jpeg);
            try
            {
                thumbnail.Save(Database.ThumbnailPath + @"\" + m_Id.ToString(), ImageFormat.Jpeg);
            }
            catch
            { 
            }

            m_Busy = false;

            return thumbnail;
        }

        internal void ShowInfo()
        {
            try
            {
                using (Image image = LoadImage())
                    MessageBox.Show(Filename + "\n" + Path + "\n" + GetExifDate(image));
            }
            catch
            {
            }
        }

        internal Photo(string filename)
        {            
            FileInfo fi = new FileInfo(filename);
            string extension = fi.Extension.ToLower();
            this.IsVideo = (extension == ".avi" || extension == ".mpg" || extension == ".mpeg" || extension == ".mp4");

            m_Id = Guid.NewGuid();
            m_Filename = fi.Name;
            m_Path = fi.DirectoryName.ToLower();
            m_FileSize = fi.Length;

            if (!this.IsVideo)            
            {
                Image image = Image.FromFile(filename);
                //string hash = Core.GetMD5Hash(File.ReadAllText(filename));
                m_Hash = Common.GetMD5HashForImage(image);
                if (m_Hash == null)
                    return;

                m_Width = (long)image.PhysicalDimension.Width;
                m_Height = (long)image.PhysicalDimension.Height;
                //FileInfo fi = new FileInfo(filename);
                
                CreateThumbnail(image);
                image.Dispose();
                image = null;
            }
        }

        /// <summary>
        /// Creates a thumbnail from image
        /// </summary>
        /// <param name="image">source image</param>
        /// <param name="size">thumbnail size</param>
        /// <returns>thumbnail image</returns>
        private static Image ThumbnailFromImage(Image image, int size)
        {
            if (image == null)
                return null;

            int width = image.Width, height = image.Height;
            decimal percentage = (width > height ? (decimal)size / width : (decimal)size / height);
            width = Convert.ToInt32((decimal)width * percentage);
            height = Convert.ToInt32((decimal)height * percentage);

            Bitmap thumbnail = new Bitmap(size, size, PixelFormat.Format24bppRgb);
            try
            {
                using (Graphics g2 = Graphics.FromImage(thumbnail))
                {
                    g2.Clear(Color.White);
                    g2.InterpolationMode = InterpolationMode.NearestNeighbor;

                    //g2.DrawImage(image, new Rectangle((size - width) / 2, (size - height) / 2, width, height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                    g2.DrawImage(image, new Rectangle((size - width) / 2, (size - height), width, height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                }
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("ThumbnailFromImage");
            }

            return thumbnail;
        }

        internal void Remove(bool recycle)
        {
            try
            {
                if (File.Exists(Database.ThumbnailPath + @"\" + m_Id.ToString()))
                    File.Delete(Database.ThumbnailPath + @"\" + m_Id.ToString());
            }
            catch 
            {                
                // TODO: merkkaa myöhemmin tuhottavaksi
            }

            if (recycle)
                Common.SendToRecycleBin(FilenameWithPath);

            Database.DeletePhoto(m_Id);
        }

        internal Image LoadThumbnail()
        {
            if (!File.Exists(Database.ThumbnailPath + @"\" + m_Id.ToString()))
                return null;

            Image image = null;
            try
            {
                image = Image.FromFile(Database.ThumbnailPath + @"\" + m_Id.ToString());
            }
            catch
            {
                return null; // tämmöne!!
            }

            if (image.Width != Thumbnails.ThumbnailSize)
            {
                try
                {
                    image.Dispose();
                    image = null;
                    File.Delete(Database.ThumbnailPath + @"\" + m_Id.ToString());
                    return null;
                }
                catch { }
            }

            return image;
        }

        internal bool Exists()
        {
            return File.Exists(FilenameWithPath);
        }

        public string GetExifDate(Image image)
        {
            foreach (PropertyItem pi in image.PropertyItems)
            {
                if (pi.Id.ToString("x") == "9003")
                    return System.Text.Encoding.Default.GetString(pi.Value);
            }

            return "";
        }

#region properties

        public bool IsVideo
        {
            get { return m_IsVideo; }
            set { m_IsVideo = value; }
        }

        public long Width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }
        public long Height
        {
            get { return m_Height; }
            set { m_Height = value; }
        }

        public long FileSize
        {
            get { return m_FileSize; }
            set { m_FileSize = value; }
        }

        internal string FilenameWithPath
        {
            get { return m_Path + @"\" +  m_Filename; }
            set { }
        }

        internal string Filename
        {
            get { return m_Filename; }
            set { m_Filename = value; }
        }

        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; }
        }

        internal string Hash
        {
            get { return m_Hash; }
            set { m_Hash = value; }
        }

        internal Guid Id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        internal DateTime ImportDate
        {
            get { return m_ImportDate; }
            set { m_ImportDate = value; }
        }
    }
#endregion

    //internal class PhotoCategory
    //{
    //    public Guid CategoryId = Guid.Empty;
    //    public bool IsAutoCategory = false;

    //    PhotoCategory(Guid categoryId, bool isAutoCategory)
    //    {
    //        CategoryId = categoryId;
    //        IsAutoCategory = isAutoCategory;
    //    }
    //}
}
