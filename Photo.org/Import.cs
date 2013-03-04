using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Data;

namespace Photo.org
{
    internal static class Import
    {
        /// <summary>
        /// Imports photos from given folder.
        /// </summary>
        /// <param name="folder">folder to get photos from</param>
        internal static void ImportFolder(string folder)
        {
            int i = 0;
            FileInfo fi = null;
            string path = "";
            Photo photo = null;
            string extension = "", lastPath = "";
            List<string> existingFiles = null;
            Guid pathId = Guid.Empty;

            Thumbnails.ClearPhotos();
            Categories.RemoveSelections();

            Status.ShowProgress();
            Database.BeginTransaction();

            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

            Status.SetMaxValue(files.Length);

            foreach (string file in files)
            {
                fi = new FileInfo(file);
                extension = fi.Extension.ToLower();
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif" || extension == ".png")
                {
                    path = fi.DirectoryName.ToLower();                   
                    if (path != lastPath)
                    {
                        Database.Commit();
                        Database.BeginTransaction();

                        existingFiles = new List<string>();

                        pathId = Database.GetPathId(path);
                        if (pathId == Guid.Empty)
                        {
                            pathId = Guid.NewGuid();
                            Database.InsertPath(pathId, path);
                        }
                        else
                        {
                            foreach (DataRow dr in Database.QueryPhotosByPathId("FILENAME", pathId).Rows)
                            {
                                existingFiles.Add(dr["FILENAME"].ToString().ToLower());
                            }
                        }
                        lastPath = path;
                    }
                    
                    if (!existingFiles.Contains(fi.Name.ToLower()))
                    {
                        photo = new Photo(file);
                        if (photo.Hash != null)
                        {
                            bool photoAlreadyExists = false;
                            if (Database.HashAlreadyExists(photo.Hash))
                                foreach (Photo p in Database.GetPhotosByHash(photo.Hash))
                                    if (!File.Exists(p.FilenameWithPath) && (p.Filename.ToLower() == photo.Filename.ToLower() || p.FileSize == photo.FileSize))
                                    {
                                        Database.UpdatePhotoLocation(p.Id, photo.Path, photo.Filename);
                                        photoAlreadyExists = true;
                                        break;
                                    }

                            if (!photoAlreadyExists)
                                Database.InsertPhoto(photo.Id, pathId, photo.Filename, photo.FileSize, photo.Hash, photo.Width, photo.Height);                            

                            //try
                            //{
                            //    Database.InsertPhoto(photo.Id, pathId, photo.Filename, photo.FileSize, photo.Hash, photo.Width, photo.Height);
                            //}
                            //catch (Exception e)
                            //{
                            //    if (e.Message.Contains("HASH"))
                            //    {
                            //        Photo p = Database.GetPhotoByHash(photo.Hash);
                            //        if (!File.Exists(p.FilenameWithPath))
                            //            Database.UpdatePhotoLocation(p.Id, photo.Path, photo.Filename);
                            //    }
                            //    else
                            //    {
                            //        throw e;
                            //    }
                            //}
                        }
                    }
                }
                Status.SetProgress(++i);
                Application.DoEvents();
            }

            Database.UpdatePhotoImportDate();

            Database.Commit();
            Status.HideProgress();
        }
    }
}
