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
            string path = "", extension = "", lastPath = "";
            Photo photo = null;           
            List<string> existingFiles = null;
            Guid pathId = Guid.Empty;
            bool hasImportInfo = false;
            DataSet importInfo = new DataSet();

            Thumbnails.ClearPhotos();
            Categories.RemoveSelections();

            Status.ShowProgress();
            Database.BeginTransaction();

            if (File.Exists(folder + (folder.EndsWith(@"\") ? "" : @"\") + "photo.org.xml"))
            {
                hasImportInfo = true;
                importInfo.ReadXml(folder + (folder.EndsWith(@"\") ? "" : @"\") + "photo.org.xml");

                foreach (DataRow dr in importInfo.Tables["Categories"].Rows)
                {
                    AddImportedCategory(dr.Table, new Guid(dr["CATEGORY_ID"].ToString()), new Guid(dr["PARENT_ID"].ToString()));
                }
            }

            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

            Status.SetMaxValue(files.Length);

            foreach (string file in files)
            {
                fi = new FileInfo(file);
                extension = fi.Extension.ToLower();
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif" || extension == ".png" || extension == ".avi" || extension == ".mpg" || extension == ".mpeg" || extension == ".mp4")
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

                        if (photo.IsVideo)
                        {
                            // GetVideosBySize

                            // ...

                            Database.InsertPhoto(photo.Id, pathId, photo.Filename, photo.FileSize, photo.Hash, photo.Width, photo.Height, photo.IsVideo);

                            // TODO
                        }
                        else
                        {
                            if (photo.Hash != null)
                            {
                                //bool photoAlreadyExists = false;
                                if (Database.HashAlreadyExists(photo.Hash))
                                {
                                    foreach (Photo p in Database.GetPhotosByHash(photo.Hash))
                                        if (!File.Exists(p.FilenameWithPath) && (p.Filename.ToLower() == photo.Filename.ToLower() || p.FileSize == photo.FileSize))
                                        {
                                            Database.UpdatePhotoLocation(p.Id, photo.Path, photo.Filename);
                                            //photoAlreadyExists = true;
                                            break;
                                        }
                                }
                                else
                                //if (!photoAlreadyExists)
                                {
                                    if (hasImportInfo)
                                    {
                                        DataRow[] dr = importInfo.Tables["Photos"].Select("FILENAME = '" + photo.Filename + "'");
                                        //if (dr.Length > 0)
                                            photo.Id = new Guid(dr[0]["PHOTO_ID"].ToString());
                                    }

                                    Database.InsertPhoto(photo.Id, pathId, photo.Filename, photo.FileSize, photo.Hash, photo.Width, photo.Height, photo.IsVideo);

                                    foreach (DataRow dr in importInfo.Tables["PhotoCategories"].Select("PHOTO_ID ='" + photo.Id.ToString() + "'"))
                                    {
                                        Categories.AddPhotoCategory(photo, new Guid(dr["CATEGORY_ID"].ToString()));
                                    }
                                }                                
                            }
                        }
                    }
                }
                Status.SetProgress(++i);
                Application.DoEvents();
            }

            Database.UpdatePhotoImportDate();

            Database.Commit();
            Status.HideProgress();
            Status.ClearTextStack();

            Categories.Refresh();
        }

        private static void AddImportedCategory(DataTable categories, Guid categoryId, Guid parentId)
        {
            if (categoryId == Guid.Empty)
                return;

            if (Categories.GetCategoryByGuid(categoryId) != null)
                return;

            if (Categories.GetCategoryByGuid(parentId) == null)
            {
                DataRow[] rows = categories.Select("CATEGORY_ID = '" + parentId.ToString() + "'");
                if (rows.Length > 0)
                    AddImportedCategory(categories, parentId, new Guid(rows[0]["PARENT_ID"].ToString()));
            }

            DataRow[] rows1 = categories.Select("CATEGORY_ID = '" + categoryId.ToString() + "'");
            Categories.AddCategory(categoryId, parentId, rows1[0]["NAME"].ToString(), long.Parse(rows1[0]["COLOR"].ToString()));
        }
    }
}
