using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace Photo.org
{
    internal static partial class Database
    {
        internal static void UpdateVersion()
        {
            //GetOne("SELECT name FROM sqlite_master WHERE type='table' AND name='PARAMETER'")

            int version = 0;

            try
            {
                version = Convert.ToInt32(GetOne("select NUMERIC_VALUE from PARAMETER where NAME = 'DATABASE_VERSION'"));
            }
            catch
            {
            }

            if (version == c_DatabaseVersion)
                return;

            FileInfo fi = new FileInfo(m_DatabaseFilename);
            string backupFilename = m_DatabaseFilename.Substring(0, m_DatabaseFilename.Length - fi.Extension.Length) + "." + DateTime.Now.ToString("ddMMyyyyHHmmss");

            //if (File.Exists(backupFilename))
            //    File.Delete(backupFilename);
            File.Copy(m_DatabaseFilename, backupFilename);               

            if (version < 1)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;
                    
                    comm.CommandText = "create table PARAMETER (NAME nvarchar(255) unique not null primary key, TEXT_VALUE nvarchar(255), NUMERIC_VALUE numeric(12,3), DATE_VALUE datetime)";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "insert into PARAMETER (NAME, NUMERIC_VALUE) values ('DATABASE_VERSION', 1)";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(1);
            }

            if (version < 2)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "delete from PHOTO_CATEGORY where PHOTO_ID in (select PHOTO_ID from PHOTO where HASH in (select HASH from PHOTO group by HASH having count(*) > 1))";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "delete from PHOTO where HASH in (select HASH from PHOTO group by HASH having count(*) > 1)";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create unique index idx_PHOTO_HASH on PHOTO (HASH desc)";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(2);
            }

            if (version < 3)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "alter table CATEGORY add SORT_ORDER integer null";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "alter table PHOTO add IMPORT_DATE timestamp null";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(3);
            }

            if (version < 4)
            {
                UpdatePhotoImportDate();
                UpdateVersionInfo(4);
            }

            if (version < 5)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "drop index idx_PHOTO_HASH";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create unique index idx_PHOTO_HASH on PHOTO (HASH desc, FILESIZE)";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(5);
            }

            if (version < 6)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "drop index idx_PHOTO_HASH";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create index idx_PHOTO_HASH on PHOTO (HASH desc)";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "alter table PHOTO add WIDTH integer null";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "alter table PHOTO add HEIGHT integer null";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "update PHOTO set HASH = null";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(6);
            }

            if (version < 7)
            {
                CreateNewHashesAndUpdateDimensions();
                UpdateVersionInfo(7);
            }

            if (version < 8)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "drop index idx_PHOTO_CATEGORY";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create unique index idx_PHOTO_CATEGORY on PHOTO_CATEGORY (PHOTO_ID, CATEGORY_ID, SOURCE);";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(8);
            }

            if (version < 9)
            {
                CreatePhotoCategoryTrigger();
                UpdateVersionInfo(9);
            }

            if (version < 10)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "create table DELETION (DELETION_ID guid not null primary key, PATH_ID guid, FILENAME nvarchar(255), RECYCLE_ONLY integer);";
                    comm.ExecuteNonQuery();                    
                }
                UpdateVersionInfo(10);
            }

            if (version < 11)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "alter table CATEGORY add COLOR integer null;";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(11);
            }

            if (version < 12)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "alter table CATEGORY add HIDDEN integer null;";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(12);
            }

            if (version < 13)
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "alter table PHOTO add IS_VIDEO integer null;";
                    comm.ExecuteNonQuery();
                }
                UpdateVersionInfo(13);
            }

            if (version < 14)
            {
                CreateNewHashesAndUpdateDimensions();
                UpdateVersionInfo(14);
            }
        }

        private static void UpdateVersionInfo(int version)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                comm.CommandText = "update PARAMETER set NUMERIC_VALUE = @numericValue where NAME = 'DATABASE_VERSION'";
                AddParameter(comm, "numericValue", DbType.Decimal).Value = version;
                comm.ExecuteNonQuery();
            }
        }

        private static void CreateNewHashesAndUpdateDimensions()
        {
            System.Windows.Forms.MessageBox.Show("Updating photo hash values next. This might take several minutes. Be patient.");

            Status.ShowProgress();

            string sql = "select ph.PHOTO_ID, ph.FILENAME, pa.PATH, ph.HASH ";
            sql += "from PHOTO ph ";
            sql += "join PATH pa on pa.PATH_ID = ph.PATH_ID";

            string hash = "";
            long filesize = 0;
            int i = 0;

            DataTable dt = Query(sql, null);
            Status.SetMaxValue(dt.Rows.Count);
            foreach (DataRow dr in dt.Rows)
                //if (dr["HASH"].ToString() == "")
                    try
                    {
                        System.Drawing.Image image = System.Drawing.Image.FromFile(dr["PATH"].ToString() + @"\" + dr["FILENAME"].ToString());                        
                        hash = Common.GetMD5HashForImage(image);
                        filesize = new System.IO.FileInfo(dr["PATH"].ToString() + @"\" + dr["FILENAME"].ToString()).Length;
                        UpdatePhoto(new Guid(dr["PHOTO_ID"].ToString()), dr["FILENAME"].ToString(), filesize, hash, (long)image.PhysicalDimension.Width, (long)image.PhysicalDimension.Height);
                        //System.Diagnostics.Trace.WriteLine(i++.ToString());
                        Status.SetProgress(++i);
                        System.Windows.Forms.Application.DoEvents();
                        image.Dispose();
                    }
                    catch
                    {
                    }

            Status.HideProgress();
        }
    }
}
