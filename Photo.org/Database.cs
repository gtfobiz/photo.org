using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Windows.Forms;

namespace Photo.org
{
    internal static partial class Database
    {
#region public interface

        internal static string ThumbnailPath
        {
            get { return Database.m_ThumbnailPath; }
            //set { Database.m_ThumbnailPath = value; }
        }

#endregion

#region private members

        private const int c_DatabaseVersion = 10;

        private static SQLiteConnection m_Connection = null;
        private static SQLiteTransaction m_Transaction = null;
        private static bool m_GotActiveTransaction = false;
        private static string m_DatabaseFilename = "";
        private static string m_ThumbnailPath = "";

#endregion
       
#region transaction

        internal static bool BeginTransaction()
        {
            if (m_Connection == null || m_GotActiveTransaction)
                return false;

            m_Transaction = m_Connection.BeginTransaction();
            m_GotActiveTransaction = true;

            return true;
        }

        internal static bool Commit()
        {
            if (m_Connection == null || !m_GotActiveTransaction)
                return false;

            m_Transaction.Commit();
            m_GotActiveTransaction = false;

            return true;
        }

        internal static bool Rollback()
        {
            if (m_Connection == null || !m_GotActiveTransaction)
                return false;

            m_Transaction.Rollback();
            m_GotActiveTransaction = false;

            return true;
        }

#endregion

#region misc

        internal static bool HasConnection
        {
            get { return m_Connection != null; }
        }

        /// <summary>
        /// Opens connection to requested database.
        /// </summary>
        /// <param name="filename">database filename</param>
        internal static void Open(string filename)
        {
            Open(filename, false);
        }

        /// <summary>
        /// Opens connection to requested database.
        /// </summary>
        /// <param name="filename">database filename</param>
        internal static void Open(string filename, bool creating)
        {
            if (m_Connection != null)
                m_Connection.Close();

            m_Connection = new SQLiteConnection("Data Source=" + filename);
            try
            {
                m_Connection.Open();
                m_GotActiveTransaction = false;
            }
            catch
            {
                m_Connection = null;
                throw;
            }

            m_DatabaseFilename = filename;
            Settings.Set(Setting.DatabaseFilename, m_DatabaseFilename);

            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            m_ThumbnailPath = filename.Substring(0, filename.Length - fi.Extension.Length) + ".thumbs";

            if (!creating)
            {
                UpdateVersion();
                Settings.ImportPath = GetParameterText("IMPORT_PATH");
                Settings.CategoryFormSearchFromBegin = (GetParameterText("CATEGORY_SEARCH_BEGIN") == "1");
            }

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                comm.CommandText = "PRAGMA recursive_triggers = true";     
                comm.ExecuteNonQuery();
            }            
        }

        private static string GetParameterText(string name)
        {
            object o = GetOne("select TEXT_VALUE from PARAMETER where name = '" + name + "'");
            if (o == null)
                return "";
            return o.ToString();
        }

        /// <summary>
        /// Closes current database connection.
        /// </summary>
        internal static void Close()
        {
            if (m_Connection != null)
            {
                m_Connection.Close();
                m_Connection = null;
            }
        }

        internal static void Dispose()
        {
            Close();
        }

        private static DbParameter AddParameter(SQLiteCommand comm, string name, DbType dbType)
        {
            DbParameter param = comm.CreateParameter();
            param.ParameterName = name;
            param.DbType = dbType;
            comm.Parameters.Add(param);
            return param;
        }

        private static object GetOne(string sql)
        {
            return new SQLiteCommand(sql, m_Connection).ExecuteScalar();
        }

        /// <summary>
        /// Executes given sql and returns query results in a datatable
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private static DataTable Query(string sql, Dictionary<string, object> parameters)
        {
            using (SQLiteCommand comm = new SQLiteCommand(sql))
            {
                if (parameters != null)
                    foreach (KeyValuePair<string, object> kvp in parameters)
                        comm.Parameters.AddWithValue(kvp.Key, kvp.Value);

                comm.Connection = m_Connection;

                SQLiteDataAdapter da = new SQLiteDataAdapter(comm);
                DataSet ds = new DataSet();

                try
                {
                    da.Fill(ds);
                    return ds.Tables[0];
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }

            return null;
        }        

#endregion

        internal static void SaveParameterText(string name, string text)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                comm.CommandText = "delete from PARAMETER where NAME = @name";
                AddParameter(comm, "name", DbType.String).Value = name;                
                comm.ExecuteNonQuery();

                comm.CommandText = "insert into PARAMETER (NAME, TEXT_VALUE) values (@name, @text)";                
                AddParameter(comm, "name", DbType.String).Value = name;
                AddParameter(comm, "text", DbType.String).Value = text;
                comm.ExecuteNonQuery();
            }
        }

#region insert methods

        /// <summary>
        /// Insert method for CATEGORY
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="parentId"></param>
        /// <param name="name"></param>
        internal static void InsertCategory(Guid categoryId, Guid parentId, string name)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "insert into CATEGORY (CATEGORY_ID, PARENT_ID, NAME) values (@categoryId, @parentId, @name)";
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                AddParameter(comm, "parentId", DbType.Guid).Value = parentId;
                AddParameter(comm, "name", DbType.String).Value = name;
                comm.ExecuteNonQuery();
            }
        }

        internal static bool InsertAutoCategory(Guid sourceId, Guid categoryId)
        {
            try
            {
                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;
                    comm.CommandText = "insert into AUTO_CATEGORY (SOURCE_ID, CATEGORY_ID) values (@sourceId, @categoryId)";
                    AddParameter(comm, "sourceId", DbType.Guid).Value = sourceId;
                    AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                    comm.ExecuteNonQuery();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Insert method for PHOTO
        /// </summary>
        /// <param name="photoId"></param>
        /// <param name="pathId"></param>
        /// <param name="filename"></param>
        /// <param name="filesize"></param>
        /// <param name="hash"></param>
        internal static void InsertPhoto(Guid photoId, Guid pathId, string filename, long filesize, string hash, long width, long height)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "insert into PHOTO (PHOTO_ID, PATH_ID, FILENAME, FILESIZE, HASH, WIDTH, HEIGHT) values (@photoId, @pathId, @filename, @filesize, @hash, @width, @height)";
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                AddParameter(comm, "pathId", DbType.Guid).Value = pathId;
                AddParameter(comm, "filename", DbType.String).Value = filename;
                AddParameter(comm, "filesize", DbType.Int64).Value = filesize;
                AddParameter(comm, "hash", DbType.String).Value = hash;
                AddParameter(comm, "width", DbType.Int64).Value = width;
                AddParameter(comm, "height", DbType.Int64).Value = height;
                comm.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert method for PATH
        /// </summary>
        /// <param name="pathId"></param>
        /// <param name="path"></param>
        internal static void InsertPath(Guid pathId, string path)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "insert into PATH (PATH_ID, PATH) values (@pathId, @path)";
                AddParameter(comm, "pathId", DbType.Guid).Value = pathId;
                AddParameter(comm, "path", DbType.String).Value = path;
                comm.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert method for CATEGORY_PATH
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="categoryId"></param>
        internal static void InsertCategoryPath(Guid targetId, Guid categoryId)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "insert into CATEGORY_PATH (TARGET_ID, CATEGORY_ID) values (@targetId, @categoryId)";
                AddParameter(comm, "targetId", DbType.Guid).Value = targetId;
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                comm.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert method for PHOTO_CATEGORY
        /// </summary>
        /// <param name="photoId"></param>
        /// <param name="categoryId"></param>
        internal static void InsertPhotoCategory(Guid photoId, Guid categoryId)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "insert into PHOTO_CATEGORY (PHOTO_ID, CATEGORY_ID, SOURCE) values (@photoId, @categoryId, @source)";
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                AddParameter(comm, "source", DbType.StringFixedLength).Value = "U";
                comm.ExecuteNonQuery();
            }
        }

#endregion

#region update methods

        /// <summary>
        /// Update method for CATEGORY
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="parentId"></param>
        /// <param name="name"></param>
        internal static void UpdateCategory(Guid categoryId, Guid parentId, string name)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "update CATEGORY set PARENT_ID = @parentId, NAME = @name where CATEGORY_ID = @categoryId";
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                AddParameter(comm, "parentId", DbType.Guid).Value = parentId;
                AddParameter(comm, "name", DbType.String).Value = name;
                comm.ExecuteNonQuery();
            }
        }

#endregion

#region delete methods

        internal static void DeleteCategoryPathByCategory(Guid categoryId)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                comm.CommandText = "delete from CATEGORY_PATH where CATEGORY_ID = @categoryId";
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                comm.ExecuteNonQuery();
            }
        }

        internal static void DeleteCategory(Guid categoryId)
        {
            bool needTransaction = false;

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                if (!m_GotActiveTransaction)
                {
                    needTransaction = true;
                    BeginTransaction();
                }

                comm.CommandText = "delete from PHOTO_CATEGORY where CATEGORY_ID = @categoryId";                
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                comm.ExecuteNonQuery();

                comm.CommandText = "delete from CATEGORY_PATH where CATEGORY_ID = @categoryId";
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                comm.ExecuteNonQuery();

                comm.CommandText = "delete from AUTO_CATEGORY where SOURCE_ID = @categoryId or CATEGORY_ID = @categoryId";
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                comm.ExecuteNonQuery();

                comm.CommandText = "delete from CATEGORY where CATEGORY_ID = @categoryId";
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                comm.ExecuteNonQuery();

                if (needTransaction)
                    Commit();
            }
        }

        internal static void DeletePhotoCategory(Guid photoId, Guid categoryId)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "delete from PHOTO_CATEGORY where PHOTO_ID = @photoId and CATEGORY_ID = @categoryId and SOURCE = @source";
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                AddParameter(comm, "source", DbType.StringFixedLength).Value = "U";
                comm.ExecuteNonQuery();

                comm.CommandText = "delete from PHOTO_CATEGORY where PHOTO_ID = @photoId and SOURCE = 'A'";
                comm.ExecuteNonQuery();


            }
        }

        internal static void DeletePhoto(Guid photoId)
        {
            bool needTranaction = false;

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                if (!m_GotActiveTransaction)
                {
                    needTranaction = true;
                    BeginTransaction();
                }

                comm.CommandText = "delete from PHOTO_CATEGORY where PHOTO_ID = @photoId";
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                comm.ExecuteNonQuery();

                comm.CommandText = "delete from PHOTO where PHOTO_ID = @photoId";
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                comm.ExecuteNonQuery();

                if (needTranaction)
                    Commit();
            }
        }

#endregion

#region query methods

        /// <summary>
        /// Searches path id by path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Guid GetPathId(string path)
        {
            object o = GetOne("select PATH_ID from PATH where PATH = '" + path.Replace("'", "''") + "'");
            return (o == null ? Guid.Empty : (Guid)o);
        }

        internal static bool CheckPhotoCategory(Guid photoId, Guid categoryId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string sql = "select 1 from PHOTO_CATEGORY where PHOTO_ID = @photoId and CATEGORY_ID = @categoryId";
            parameters.Add("@photoId", photoId);
            parameters.Add("@categoryId", categoryId);

            return (Query(sql, parameters).Rows.Count > 0);
        }

        internal static DataTable QueryPhotosByPathId(string fields, Guid pathId)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string sql = "select " + fields + " from PHOTO where PATH_ID = @pathId";
            parameters.Add("@pathId", pathId);

            return Query(sql, parameters);
        }

        /// <summary>
        /// Fetches photos and its categories based on category selections.
        /// </summary>
        /// <param name="categories"></param>
        /// <returns>dataset containing tables for photos and categories</returns>
        internal static DataSet QueryPhotosByCategories(List<Guid> categories)
        {
            string requiredSql = "";

            if (categories == null)
                categories = new List<Guid>();
            
            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i] == Guids.Unassigned)
                {
                    requiredSql += (i == 0 ? "" : " and ");
                    requiredSql += "not exists(select 1 from PHOTO_CATEGORY pcx where PHOTO_ID = ph.PHOTO_ID)";                    
                }
                else
                {
                    requiredSql += (i == 0 ? "" : " and ");
                    requiredSql += "exists(select 1 from PHOTO_CATEGORY pcx ";
                    requiredSql += "join CATEGORY_PATH cpx on cpx.CATEGORY_ID = pcx.CATEGORY_ID ";
                    requiredSql += "where pcx.PHOTO_ID = ph.PHOTO_ID and cpx.TARGET_ID = @required" + i.ToString() + ")";
                }
            }

            if (!Status.ShowHiddenPhotos)
            {
                requiredSql += (requiredSql == "" ? "" : " and ");
                requiredSql += "not exists(select 1 from PHOTO_CATEGORY ";
                requiredSql += "where PHOTO_ID = ph.PHOTO_ID and CATEGORY_ID = @hidden)";
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string sql = "select ph.PHOTO_ID, pa.PATH, ph.FILENAME, ph.FILESIZE, pc.CATEGORY_ID, ph.IMPORT_DATE, pc.SOURCE ";
            sql += "from PHOTO ph ";
            sql += "join PATH pa on pa.PATH_ID = ph.PATH_ID ";
            sql += "left join PHOTO_CATEGORY pc on pc.PHOTO_ID = ph.PHOTO_ID ";
            if (requiredSql != "")
                sql += " where " + requiredSql;
            sql += " order by ph.PHOTO_ID";

            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i] != Guids.Unassigned)
                    parameters.Add("@required" + i.ToString(), categories[i]);
            }

            if (!Status.ShowHiddenPhotos)
            {
                parameters.Add("@hidden", Guids.Hidden);
            }

            DataTable dt = Query(sql, parameters);
            DataSet ds = new DataSet();

            ds.Tables.Add("Photos");
            DataColumnCollection dcc = ds.Tables["Photos"].Columns;
            dcc.Add("PHOTO_ID");
            dcc.Add("PATH");
            dcc.Add("FILENAME");
            dcc.Add("FILESIZE");
            dcc.Add("IMPORT_DATE");

            ds.Tables.Add("Categories");
            dcc = ds.Tables["Categories"].Columns;
            dcc.Add("PHOTO_ID");
            dcc.Add("CATEGORY_ID");
            dcc.Add("SOURCE");

            Guid photoId = Guid.Empty, lastPhotoId = Guid.Empty;
            DataRow resultRow = null;

            foreach (DataRow dr in dt.Rows)
            { 
                photoId = new Guid(dr["PHOTO_ID"].ToString());
                if (photoId != lastPhotoId)
                {
                    lastPhotoId = photoId;

                    resultRow = ds.Tables["Photos"].NewRow();
                    resultRow["PHOTO_ID"] = dr["PHOTO_ID"];
                    resultRow["PATH"] = dr["PATH"];
                    resultRow["FILENAME"] = dr["FILENAME"];
                    resultRow["FILESIZE"] = dr["FILESIZE"];
                    resultRow["IMPORT_DATE"] = dr["IMPORT_DATE"];
                    ds.Tables["Photos"].Rows.Add(resultRow);                    
                }

                if (dr["CATEGORY_ID"].ToString() != "")
                {
                    resultRow = ds.Tables["Categories"].NewRow();
                    resultRow["PHOTO_ID"] = dr["PHOTO_ID"];
                    resultRow["CATEGORY_ID"] = dr["CATEGORY_ID"];
                    resultRow["SOURCE"] = dr["SOURCE"];
                    ds.Tables["Categories"].Rows.Add(resultRow);
                }
            }

            return ds;
        }

        internal static DataTable QueryCategories()
        {            
            //string sql = "select c.CATEGORY_ID, c.PARENT_ID, c.NAME, count(distinct pc.PHOTO_ID) as PHOTO_COUNT ";
            //sql += "from CATEGORY c ";
            //sql += "left join CATEGORY_PATH cp on cp.TARGET_ID = c.CATEGORY_ID ";
            //sql += "left join PHOTO_CATEGORY pc on pc.CATEGORY_ID = cp.CATEGORY_ID ";
            //sql += "group by c.NAME ";
            //sql += "order by c.NAME";

            string sql = "select CATEGORY_ID, PARENT_ID, NAME, 0 as PHOTO_COUNT from CATEGORY order by NAME";
            return Query(sql, null);
        }

#endregion        

#region database handling

        /// <summary>
        /// Creates an empty database with given filename
        /// </summary>
        /// <param name="filename">database filename</param>
        internal static void Create(string filename)
        {
            try
            {
                Status.ShowProgress();

                Close();
                if (System.IO.File.Exists(filename))
                    System.IO.File.Delete(filename);

                Open(filename, true);

                using (SQLiteCommand comm = new SQLiteCommand())
                {
                    comm.Connection = m_Connection;

                    comm.CommandText = "create table CATEGORY (CATEGORY_ID guid not null primary key, PARENT_ID guid null, NAME nvarchar(50), SORT_ORDER integer null);";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_CATEGORY on CATEGORY (CATEGORY_ID);";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table PATH (PATH_ID guid not null primary key, PATH nvarchar(255));";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_PATH on PATH (PATH_ID);";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table PHOTO (PHOTO_ID guid not null primary key, PATH_ID guid, FILENAME nvarchar(255), FILESIZE long, HASH nvarchar(32), IMPORT_DATE timestamp null, WIDTH integer null, HEIGHT integer null);";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_PHOTO on PHOTO (PHOTO_ID);";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create index idx_PHOTO_HASH on PHOTO (HASH desc)";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table PHOTO_CATEGORY (PHOTO_ID guid, CATEGORY_ID guid, SOURCE char(1));";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_PHOTO_CATEGORY on PHOTO_CATEGORY (PHOTO_ID, CATEGORY_ID, SOURCE);";
                    comm.ExecuteNonQuery();                    

                    comm.CommandText = "create table CATEGORY_PATH (TARGET_ID guid, CATEGORY_ID guid);";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_CATEGORY_PATH on CATEGORY_PATH (TARGET_ID, CATEGORY_ID);";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table AUTO_CATEGORY (SOURCE_ID guid, CATEGORY_ID guid);";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_AUTO_CATEGORY on AUTO_CATEGORY (SOURCE_ID, CATEGORY_ID);";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table PARAMETER (NAME nvarchar(255) unique not null primary key, TEXT_VALUE nvarchar(255), NUMERIC_VALUE numeric(12,3), DATE_VALUE datetime)";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "insert into PARAMETER (NAME, NUMERIC_VALUE) values ('DATABASE_VERSION', @numericValue)";
                    AddParameter(comm, "numericValue", DbType.Decimal).Value = c_DatabaseVersion;
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table DELETION (DELETION_ID guid not null primary key, PATH_ID guid, FILENAME nvarchar(255), RECYCLE_ONLY integer);";
                    comm.ExecuteNonQuery();

                    CreatePhotoCategoryTrigger();
                    Categories.InsertTestCategories();
                }

                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(m_ThumbnailPath);
                di.Create();
            }
            catch
            {
                throw;
            }
            finally
            {
                Status.HideProgress();
            }
        }

        /// <summary>
        /// Checks that required database files exist.
        /// </summary>
        internal static void CheckDatabaseFiles()
        {
            try
            {
                if (System.IO.File.Exists(m_DatabaseFilename))
                    Open(m_DatabaseFilename);
                else
                    Create(m_DatabaseFilename);

                if (!System.IO.Directory.Exists(m_ThumbnailPath))
                    new System.IO.DirectoryInfo(m_ThumbnailPath).Create();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        internal static void OpenDefaultDatabase()
        {
            try
            {
                m_DatabaseFilename = Settings.Get(Setting.DatabaseFilename);
                if (m_DatabaseFilename == "")
                    return;

                if (!System.IO.File.Exists(m_DatabaseFilename))
                    return;

                Thumbnails.ClearPhotos();
                CheckDatabaseFiles();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        internal static void SelectDatabaseFile()
        {
            OpenFileDialog o = new OpenFileDialog();
            o.Title = "Select database file";
            o.InitialDirectory = ".";
            o.CheckFileExists = false;
            o.Filter = "Database files (*.db)|*.db";
            if (o.ShowDialog() != DialogResult.OK)
                return;

            m_DatabaseFilename = o.FileName;
            CheckDatabaseFiles();            
        }

#endregion

        internal static List<Photo> GetPhotosByHash(string hash)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            string sql = "select ph.PHOTO_ID, ph.FILENAME, pa.PATH, ph.FILESIZE, ph.HASH from PHOTO ph ";
            sql += "join PATH pa on pa.PATH_ID = ph.PATH_ID ";
            sql += "where ph.HASH = @hash";
            
            parameters.Add("@hash", hash);

            DataTable dt = Query(sql, parameters);

            if (dt == null)
                return null;

            List<Photo> photos = new List<Photo>();
            foreach (DataRow dr in dt.Rows)
            {
                Photo photo = new Photo();
                photo.Id = new Guid(dr["PHOTO_ID"].ToString());
                photo.Filename = dr["FILENAME"].ToString();
                photo.Path = dr["PATH"].ToString();
                photo.FileSize = Convert.ToInt64(dr["FILESIZE"]);
                photo.Hash = dr["HASH"].ToString();
                photos.Add(photo);
            }

            return photos;
        }

        internal static void UpdatePhoto(Guid photoId, string filename, long filesize, string hash, long width, long height)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "update PHOTO set FILENAME = @filename, FILESIZE = @filesize, HASH = @hash, WIDTH = @width, HEIGHT = @height where PHOTO_ID = @photoId";
                AddParameter(comm, "filename", DbType.String).Value = filename;
                AddParameter(comm, "filesize", DbType.Int64).Value = filesize;
                AddParameter(comm, "hash", DbType.String).Value = hash;
                AddParameter(comm, "width", DbType.Int64).Value = width;
                AddParameter(comm, "height", DbType.Int64).Value = height;
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                comm.ExecuteNonQuery();
            }
        }

        internal static void UpdatePhotoLocation(Guid photoId, string path, string filename)
        {
            Guid pathId = GetPathId(path);

            if (pathId == Guid.Empty)
            {
                pathId = Guid.NewGuid();
                InsertPath(pathId, path);
            }

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "update PHOTO set PATH_ID = @pathId, FILENAME = @filename where PHOTO_ID = @photoId";
                AddParameter(comm, "pathId", DbType.Guid).Value = pathId;
                AddParameter(comm, "filename", DbType.String).Value = filename;
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;                
                comm.ExecuteNonQuery();
            }
        }

        internal static void UpdatePhotoImportDate()
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "update PHOTO set IMPORT_DATE = datetime('now') where IMPORT_DATE is null";
                comm.ExecuteNonQuery();
            }
        }

        internal static void ApplyAutoCategories()
        {
            ApplyAutoCategories(Guid.Empty);
        }

        internal static void ApplyAutoCategories(Guid sourceId)
        {
            ApplyAutoCategories(Guid.Empty, sourceId);
        }

        internal static void ApplyAutoCategories(Guid photoId, Guid sourceId)
        {
            Status.Busy = true;

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                do
                {
                    string sql = "insert or ignore into PHOTO_CATEGORY (PHOTO_ID, CATEGORY_ID, SOURCE) ";
                    sql += "select distinct ";
                    sql += "pc.PHOTO_ID, ac.CATEGORY_ID, 'A' ";
                    sql += "from PHOTO_CATEGORY pc ";
                    sql += "join CATEGORY_PATH cp on cp.CATEGORY_ID = pc.CATEGORY_ID ";
                    sql += "join AUTO_CATEGORY ac on ac.SOURCE_ID = cp.TARGET_ID ";
                    if (photoId != Guid.Empty)
                        sql += "and pc.PHOTO_ID = @photoId ";
                    if (sourceId != Guid.Empty)
                        sql += "and ac.SOURCE_ID = @sourceId ";
                    sql += "where not exists(select 1 from PHOTO_CATEGORY x where x.PHOTO_ID = pc.PHOTO_ID and x.CATEGORY_ID = ac.CATEGORY_ID and x.SOURCE = 'A')";

                    comm.CommandText = sql;
                    if (photoId != Guid.Empty)
                        AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                    if (sourceId != Guid.Empty)
                        AddParameter(comm, "sourceId", DbType.Guid).Value = sourceId;
                } while (comm.ExecuteNonQuery() > 0);
            }

            Status.Busy = false;
        }

        internal static void DropPhotoCategoryTrigger()
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "drop trigger TR_PHOTO_CATEGORY";
                comm.ExecuteNonQuery();
            }
        }

        internal static void CreatePhotoCategoryTrigger()
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                string sql = "CREATE TRIGGER TR_PHOTO_CATEGORY AFTER INSERT ON PHOTO_CATEGORY FOR EACH ROW ";
                sql += "BEGIN ";
                sql += "INSERT INTO PHOTO_CATEGORY (PHOTO_ID, CATEGORY_ID, SOURCE) ";
                sql += "SELECT NEW.PHOTO_ID, ac.CATEGORY_ID, 'A' ";
                sql += "FROM CATEGORY_PATH cp JOIN AUTO_CATEGORY ac ON ac.SOURCE_ID = cp.CATEGORY_ID ";
                sql += "WHERE cp.TARGET_ID = NEW.CATEGORY_ID AND NOT EXISTS (";
                sql += "SELECT 1 FROM PHOTO_CATEGORY WHERE PHOTO_ID = NEW.PHOTO_ID AND CATEGORY_ID = ac.CATEGORY_ID AND SOURCE = 'A'); ";
                sql += "END;";

                comm.Connection = m_Connection;                
                comm.CommandText = sql;
                comm.ExecuteNonQuery();
            }
        }

        internal static void DoMaintenance()
        {            
            Status.Busy = true;            

            //todo:
            //pragma integrity_check
            //-      
            string sql = "";      
            int orphanBranches = 0, orphanThumbFiles = 0, obsoletePaths = 0;

            DropPhotoCategoryTrigger();
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                sql = "delete from PHOTO_CATEGORY where SOURCE = 'A'";

                comm.Connection = m_Connection;
                comm.CommandText = sql;
                comm.ExecuteNonQuery();
            }
            ApplyAutoCategories();
            CreatePhotoCategoryTrigger();

            // connect orphan branches to root
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                sql = "update CATEGORY set PARENT_ID = @guidEmpty where PARENT_ID in (";
                sql += "select distinct c.PARENT_ID from CATEGORY c ";
                sql += "left join CATEGORY p on p.CATEGORY_ID = c.PARENT_ID ";
                sql += "where p.CATEGORY_ID is null and c.PARENT_ID is not null and c.PARENT_ID <> @guidEmpty)";

                comm.Connection = m_Connection;
                comm.CommandText = sql;
                AddParameter(comm, "guidEmpty", DbType.Guid).Value = Guid.Empty;
                orphanBranches = comm.ExecuteNonQuery();
            }

            // delete orphan thumbnail files
            DataTable dt = Query("select PHOTO_ID from PHOTO", null);
            string[] thumbs = System.IO.Directory.GetFiles(m_ThumbnailPath);
            foreach (string thumb in thumbs)
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(thumb);
                if (dt.Select("PHOTO_ID = '" + fi.Name + "'").Length == 0)
                    try
                    {
                        orphanThumbFiles++;
                        fi.Delete();
                    }
                    catch
                    {
                        // todo
                    }
            }

            // remove paths without photos
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                sql = "delete from PATH where PATH_ID in (select p.PATH_ID from PATH p where not exists (select 1 from PHOTO where PATH_ID = p.PATH_ID))";
                comm.Connection = m_Connection;
                comm.CommandText = sql;
                obsoletePaths = comm.ExecuteNonQuery();
            }         

            //rebuild categorypath
            //backup

            //select pc.CATEGORY_ID photo_category pc left join category c on c.CATEGORY_ID = pc.CATEGORY_ID where c.CATEGORY_ID is null

            //-statistics

            MessageBox.Show(
                orphanBranches.ToString() + " orphan branches\n" +
                orphanThumbFiles.ToString() + " orphan thumbnail files\n" +
                obsoletePaths.ToString() + " obsolete paths");

            Status.Busy = false;
        }

        internal static bool HashAlreadyExists(string hash)
        {
            object o = GetOne("select 1 from PHOTO where HASH = '" + hash + "'");
            return (o == null || o.ToString() == "1");
        }

        internal static void QueryPhotoCategories(Photo photo)
        {
            string sql = "select pc.CATEGORY_ID, pc.SOURCE ";
            sql += "from PHOTO_CATEGORY pc ";
            sql += "where pc.PHOTO_ID = @photoId";

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@photoId", photo.Id);

            photo.Categories = new List<Guid>();
            photo.AutoCategories = new List<Guid>();

            foreach (DataRow dr in Query(sql, parameters).Rows)
                (dr["SOURCE"].ToString() == "U" ? photo.Categories : photo.AutoCategories).Add(new Guid(dr["CATEGORY_ID"].ToString()));
        }
    }
}
