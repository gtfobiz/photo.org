using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Drawing;

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

        private static bool m_IsValidPassword = false;

#endregion

#region private members

        private const int c_DatabaseVersion = 14;

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
                CheckPassword();
            }

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                comm.CommandText = "PRAGMA recursive_triggers = true";     
                comm.ExecuteNonQuery();
            }

            Common.SetFormCaption("Photo.org  --  " + m_DatabaseFilename);
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
        internal static void InsertCategory(Guid categoryId, Guid parentId, string name, Color color)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "insert into CATEGORY (CATEGORY_ID, PARENT_ID, NAME, COLOR) values (@categoryId, @parentId, @name, @color)";
                AddParameter(comm, "categoryId", DbType.Guid).Value = categoryId;
                AddParameter(comm, "parentId", DbType.Guid).Value = parentId;
                AddParameter(comm, "name", DbType.String).Value = name;

                if (color == Color.Empty)
                    AddParameter(comm, "color", DbType.Int32).Value = DBNull.Value;
                else
                    AddParameter(comm, "color", DbType.Int32).Value = color.ToArgb();

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
        internal static void InsertPhoto(Guid photoId, Guid pathId, string filename, long filesize, string hash, long width, long height, bool isVideo)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "insert into PHOTO (PHOTO_ID, PATH_ID, FILENAME, FILESIZE, HASH, WIDTH, HEIGHT, IS_VIDEO) values (@photoId, @pathId, @filename, @filesize, @hash, @width, @height, @isVideo)";
                AddParameter(comm, "photoId", DbType.Guid).Value = photoId;
                AddParameter(comm, "pathId", DbType.Guid).Value = pathId;
                AddParameter(comm, "filename", DbType.String).Value = filename;
                AddParameter(comm, "filesize", DbType.Int64).Value = filesize;
                AddParameter(comm, "hash", DbType.String).Value = hash;
                AddParameter(comm, "width", DbType.Int64).Value = width;
                AddParameter(comm, "height", DbType.Int64).Value = height;
                AddParameter(comm, "isVideo", DbType.Int64).Value = (isVideo ? "1" : "0");
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

        internal static void UpdateCategory(Category category)
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "update CATEGORY set PARENT_ID = @parentId, NAME = @name, COLOR = @color where CATEGORY_ID = @categoryId";
                AddParameter(comm, "categoryId", DbType.Guid).Value = category.Id;
                AddParameter(comm, "parentId", DbType.Guid).Value = category.ParentId;
                AddParameter(comm, "name", DbType.String).Value = category.Name;
                if (category.Color == Color.Empty)
                    AddParameter(comm, "color", DbType.Int32).Value = DBNull.Value;
                else
                    AddParameter(comm, "color", DbType.Int32).Value = category.Color.ToArgb();
                comm.ExecuteNonQuery();
            }
        }

#endregion

#region delete methods

        internal static void DeleteAllCategoryPaths()
        {
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;

                comm.CommandText = "delete from CATEGORY_PATH";
                comm.ExecuteNonQuery();
            }
        }

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

        internal static DataSet QueryHiddenPhotos()
        {
            return QueryPhotosBySqlWhere("#HIDDEN", null);
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

        internal static DataSet QueryPhotosByGuid(string searchString)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            try
            {
                parameters.Add("@guid", new Guid(searchString));
            }
            catch
            {
                return null;
            }
            string where = "ph.PHOTO_ID = @guid";
            return QueryPhotosBySqlWhere(where, parameters);
        }

        internal static DataSet QueryDuplicateHashes()
        {
            string where = "HASH in (select HASH from PHOTO group by HASH having count(*) > 1)";
            return QueryPhotosBySqlWhere(where, null, "ph.HASH");
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

        private static DataSet QueryPhotosBySqlWhere(string where, Dictionary<string, object> parameters)
        {
            return QueryPhotosBySqlWhere(where, parameters, null);
        }

        private static DataSet QueryPhotosBySqlWhere(string where, Dictionary<string, object> parameters, string orderBy)
        {
            if (!Status.ShowHiddenPhotos)
            {
                where += (where == "" ? "" : " and ");
                where += "not exists(select 1 from PHOTO_CATEGORY ";
                where += "where PHOTO_ID = ph.PHOTO_ID and CATEGORY_ID = @hidden)";
            }
            else
                if (where == "#HIDDEN")
                {
                    where = "exists(select 1 from PHOTO_CATEGORY ";
                    where += "where PHOTO_ID = ph.PHOTO_ID and CATEGORY_ID = @hidden)";
                    parameters = new Dictionary<string, object>();
                    parameters.Add("@hidden", Guids.Hidden);
                }

            string sql = "select ph.PHOTO_ID, pa.PATH, ph.FILENAME, ph.FILESIZE, pc.CATEGORY_ID, ph.IMPORT_DATE, pc.SOURCE, ph.IS_VIDEO ";
            sql += "from PHOTO ph ";
            sql += "join PATH pa on pa.PATH_ID = ph.PATH_ID ";
            sql += "left join PHOTO_CATEGORY pc on pc.PHOTO_ID = ph.PHOTO_ID ";
            if (where != "")
                sql += " where " + where;
            if (orderBy == null)
                sql += " order by ph.PHOTO_ID";  // order by RANDOM()   LIMIT 10
            else
                sql += " order by " + orderBy;

            if (!Status.ShowHiddenPhotos)
            {
                parameters.Add("@hidden", Guids.Hidden);
            }

            DataTable dt = Query(sql, parameters);
            DataSet ds = new DataSet();

            ds.Tables.Add("Photos");
            DataColumnCollection dcc = ds.Tables["Photos"].Columns;
            dcc.Add("PHOTO_ID");
            dcc.Add("IS_VIDEO");
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
                    resultRow["IS_VIDEO"] = dr["IS_VIDEO"];
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

        internal static DataSet QueryPhotosByFilename(string searchString)
        {            
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@filename", "%" + searchString + "%");
            string where = "FILENAME like @filename";
            return QueryPhotosBySqlWhere(where, parameters);
        }

        /// <summary>
        /// Fetches photos and its categories based on category selections.
        /// </summary>
        /// <param name="categories"></param>
        /// <returns>dataset containing tables for photos and categories</returns>
        internal static DataSet QueryPhotosByCategories(List<Guid> categories, bool invertedSearch)
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
                    requiredSql += (invertedSearch ? "not " : "");
                    requiredSql += "exists(select 1 from PHOTO_CATEGORY pcx ";
                    requiredSql += "join CATEGORY_PATH cpx on cpx.CATEGORY_ID = pcx.CATEGORY_ID ";
                    requiredSql += "where pcx.PHOTO_ID = ph.PHOTO_ID and cpx.TARGET_ID = @required" + i.ToString() + ")";
                }
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();

            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i] != Guids.Unassigned)
                    parameters.Add("@required" + i.ToString(), categories[i]);
            }

            return QueryPhotosBySqlWhere(requiredSql, parameters);            
        }

        //TODO: importin aikana ja jälkeen näkyy alhaal kuvien aikaisempi määrä

        private static void CheckPassword()
        {
            //m_IsValidPassword = false;

            //if (!Status.ShowHiddenCategories)
            //    return;

            //string result = Common.InputBox("testi");
            //if (result != null)
            //    MessageBox.Show(result);

            m_IsValidPassword = true;
        }

        internal static DataTable QueryCategories()
        {
            string sql = "select CATEGORY_ID, PARENT_ID, NAME, COLOR from CATEGORY ";
            if (!Status.ShowHiddenCategories || !m_IsValidPassword)
                sql += "where HIDDEN is null or HIDDEN = 0 ";
            sql += "order by NAME";

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

                    comm.CommandText = "create table CATEGORY (CATEGORY_ID guid not null primary key, PARENT_ID guid null, NAME nvarchar(50), SORT_ORDER integer null, COLOR integer null, HIDDEN integer null);";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_CATEGORY on CATEGORY (CATEGORY_ID);";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table PATH (PATH_ID guid not null primary key, PATH nvarchar(255));";
                    comm.ExecuteNonQuery();
                    comm.CommandText = "create unique index idx_PATH on PATH (PATH_ID);";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "create table PHOTO (PHOTO_ID guid not null primary key, PATH_ID guid, FILENAME nvarchar(255), FILESIZE long, HASH nvarchar(32), IMPORT_DATE timestamp null, WIDTH integer null, HEIGHT integer null, IS_VIDEO integer null);";
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
                m_DatabaseFilename = Common.CommandLineDatabaseFilename;

                if (m_DatabaseFilename == "")
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

        internal static void UpdateRootFolder(Dictionary<Guid, string> paths, string oldRoot, string newRoot)
        {
            string path = "";

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "update PATH set PATH = @path where PATH_ID = @pathId";                

                foreach (KeyValuePair<Guid, string> kvp in paths)                
                    if (kvp.Value.StartsWith(oldRoot))
                    {
                        path = kvp.Value.Replace(oldRoot, newRoot);

                        comm.Parameters.Clear();
                        AddParameter(comm, "path", DbType.String).Value = path;
                        AddParameter(comm, "pathId", DbType.Guid).Value = kvp.Key;                    

                        comm.ExecuteNonQuery();
                    }
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
                sql += "FROM CATEGORY_PATH cp JOIN AUTO_CATEGORY ac ON ac.SOURCE_ID = cp.TARGET_ID ";
                sql += "WHERE cp.CATEGORY_ID = NEW.CATEGORY_ID AND NOT EXISTS (";
                sql += "SELECT 1 FROM PHOTO_CATEGORY WHERE PHOTO_ID = NEW.PHOTO_ID AND CATEGORY_ID = ac.CATEGORY_ID AND SOURCE = 'A'); ";
                sql += "END;";

                comm.Connection = m_Connection;                
                comm.CommandText = sql;
                comm.ExecuteNonQuery();
            }
        }

        internal static void RehashPhotos()
        {
            //CreateNewHashesAndUpdateDimensions(); // fixaa tuolta kommentti pois

            //Image image1 = Image.FromFile(@"d:\backup folder\photos\_images_\perhe\2004\hehheh-161204.jpg");
            //Image image2 = Image.FromFile(@"d:\backup folder\photos\_images_\perhe\2003\v_023-170703.jpg");
            //Image image3 = Image.FromFile(@"d:\backup folder\photos\_images_\perhe\(muut)\annatulla.jpg");
            //Image image4 = Image.FromFile(@"d:\backup folder\photos\_images_\perhe\(muut)\jorgos.jpg");
            //Image image5 = Image.FromFile(@"d:\backup folder\photos\_images_\perhe\(muut)\mummola04.jpg");
            //Image image6 = Image.FromFile(@"d:\backup folder\photos\_images_\perhe\(muut)\santa.JPG");

            //System.Diagnostics.Trace.WriteLine("1: " + Common.GetMD5HashForImage((Image)image1.Clone()));
            //System.Diagnostics.Trace.WriteLine("2: " + Common.GetMD5HashForImage((Image)image2.Clone()));
            //System.Diagnostics.Trace.WriteLine("3: " + Common.GetMD5HashForImage((Image)image3.Clone()));
            //System.Diagnostics.Trace.WriteLine("4: " + Common.GetMD5HashForImage((Image)image4.Clone()));
            //System.Diagnostics.Trace.WriteLine("5: " + Common.GetMD5HashForImage((Image)image5.Clone()));
            //System.Diagnostics.Trace.WriteLine("6: " + Common.GetMD5HashForImage((Image)image6.Clone()));
            //System.Diagnostics.Trace.WriteLine("");


            //return;

            string sql = "";
            string files = "";

            sql = "select ph.PHOTO_ID, pa.PATH, ph.FILENAME, ph.HASH, ph.FILESIZE ";
            sql += "from PHOTO ph ";
            sql += "join PATH pa on pa.PATH_ID = ph.PATH_ID";

            DataTable dt = Query(sql, null);
            int eq = 0, neq = 0;

            foreach (DataRow dr in dt.Rows)
            {
                string filename = dr["PATH"].ToString();
                if (!filename.EndsWith(@"\"))
                    filename += @"\";
                filename += dr["FILENAME"].ToString();

                Image image = Image.FromFile(filename);
                string Hash = Common.GetMD5HashForImage(image);
                if (Hash == null)
                    continue;

                //UpdatePhoto((Guid)dr["PHOTO_ID"], dr["FILENAME"], )

                if (Hash == dr["HASH"].ToString())
                    eq++;
                else
                {
                    neq++;
                    files += filename + "\n";
                }
            }

            //eq = 10210, neq = 9671, maxdate = 20141126
            //filesize eq = 19778, neq = 103        
            //uusiksajo -> eq = 19571, neq = 310
            //seuraava ilman muutoksia -> eq = 19677, neq = 204

            int x = eq;
        }

        internal static void DoMaintenance()
        {            
            Status.Busy = true;

            //todo:
            //pragma integrity_check
            //-      
            string sql = "";      
            int orphanBranches = 0, orphanThumbFiles = 0, obsoletePaths = 0;

            Status.PushText();
            //Status.SetText("Assigning derived categories... [1/4]");
            //DropPhotoCategoryTrigger();

            //Status.SetText("Assigning derived categories... [2/4]");
            //using (SQLiteCommand comm = new SQLiteCommand())
            //{
            //    sql = "delete from PHOTO_CATEGORY where SOURCE = 'A'";

            //    comm.Connection = m_Connection;
            //    comm.CommandText = sql;
            //    comm.ExecuteNonQuery();
            //}

            //Status.SetText("Assigning derived categories... [3/4]");
            //ApplyAutoCategories();
            //Status.SetText("Assigning derived categories... [4/4]");
            //CreatePhotoCategoryTrigger();

            Status.SetText("Handling orphan category branches...");

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

            Status.SetText("Fixing empty category names...");
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                sql = "update CATEGORY set NAME = '(empty)' where NAME = ''";

                comm.Connection = m_Connection;
                comm.CommandText = sql;
                comm.ExecuteNonQuery();
            }

            Status.SetText("Deleting orphan thumbnail files...");

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

            Status.SetText("Deleting paths without photos...");

            // remove paths without photos
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                sql = "delete from PATH where PATH_ID in (select p.PATH_ID from PATH p where not exists (select 1 from PHOTO where PATH_ID = p.PATH_ID))";
                comm.Connection = m_Connection;
                comm.CommandText = sql;
                obsoletePaths = comm.ExecuteNonQuery();
            }

            Status.SetText("Rebuilding category paths...");
            Categories.RebuildCategoryPaths();

            //backup

            //select pc.CATEGORY_ID photo_category pc left join category c on c.CATEGORY_ID = pc.CATEGORY_ID where c.CATEGORY_ID is null

            //-statistics

            Status.SetText("Compacting database...");

            using (SQLiteCommand comm = new SQLiteCommand())
            {
                comm.Connection = m_Connection;
                comm.CommandText = "vacuum";
                comm.ExecuteNonQuery();
            }

            Status.PopText();

            MessageBox.Show(
                orphanBranches.ToString() + " orphan branches\n" +
                orphanThumbFiles.ToString() + " orphan thumbnail files\n" +
                obsoletePaths.ToString() + " obsolete paths");

            Status.Busy = false;
        }

        internal static bool HashAlreadyExists(string hash)
        {
            object o = GetOne("select 1 from PHOTO where HASH = '" + hash + "'");

            if (o == null)
                return false;

            return (o.ToString() == "1");

            //return (o == null || o.ToString() == "1");
        }

        internal static Dictionary<Guid, string> QueryPaths()
        {
            Dictionary<Guid, string> paths = new Dictionary<Guid, string>();

            string sql = "select p.PATH_ID, p.PATH ";
            sql += "from PATH p ";
            sql += "order by p.PATH";

            Dictionary<string, object> parameters = new Dictionary<string, object>();

            foreach (DataRow dr in Query(sql, parameters).Rows)
                paths.Add((Guid)dr["PATH_ID"], dr["PATH"].ToString());

            return paths;
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

        internal static void ShowStatistics()
        {
            Status.Busy = true;

            long photoCount = (long)GetOne("select count(*) from PHOTO");
            long unassignedPhotoCount = (long)GetOne("select count(*) from PHOTO where not exists(select 1 from PHOTO_CATEGORY where PHOTO_ID = PHOTO.PHOTO_ID)");
            long hiddenPhotoCount = 0;
            long categoryCount = (long)GetOne("select count(*) from CATEGORY");
            long photoCategoryCount = 0;
            decimal categoriesPerPhotoAvg = 0;
            long categoriesPerPhotoMax = 0;

            string sql = "select count(*) from PHOTO_CATEGORY where CATEGORY_ID <> @hidden";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@hidden", Guids.Hidden);
            photoCategoryCount = (long)Query(sql, parameters).Rows[0][0];
            if (photoCount - unassignedPhotoCount > 0)
                categoriesPerPhotoAvg = (decimal)photoCategoryCount / (photoCount - unassignedPhotoCount);

            sql = "select PHOTO_ID, count(*) from PHOTO_CATEGORY where CATEGORY_ID <> @hidden group by PHOTO_ID order by count(*) desc";
            categoriesPerPhotoMax = (long)Query(sql, parameters).Rows[0][1];

            if (Status.ShowHiddenPhotos)
            {
                sql = "select count(*) from PHOTO where exists(select 1 from PHOTO_CATEGORY where PHOTO_ID = PHOTO.PHOTO_ID and CATEGORY_ID = @hidden)";
                hiddenPhotoCount = (long)Query(sql, parameters).Rows[0][0];
            }

            string statistics =
                "number of photos\t\t" + photoCount.ToString()
                + "\n- unassigned\t\t" + unassignedPhotoCount.ToString()
                + (Status.ShowHiddenPhotos ? "\n- hidden\t\t\t" + hiddenPhotoCount.ToString() : "")
                + "\n\nnumber of categories\t" + categoryCount.ToString()
                + "\n- max per photo\t\t" + categoriesPerPhotoMax.ToString()
                + "\n- avg per photo\t\t" + categoriesPerPhotoAvg.ToString("0.00");
                

            Status.Busy = false;
            MessageBox.Show(statistics, "Statistics");
        }

        internal static void RepairAutoCategories()
        {
            Status.Busy = true;

            //todo:
            //pragma integrity_check
            //-      
            string sql = "";            

            Status.PushText();
            Status.SetText("Assigning derived categories... [1/4]");
            DropPhotoCategoryTrigger();

            Status.SetText("Assigning derived categories... [2/4]");
            using (SQLiteCommand comm = new SQLiteCommand())
            {
                sql = "delete from PHOTO_CATEGORY where SOURCE = 'A'";

                comm.Connection = m_Connection;
                comm.CommandText = sql;
                comm.ExecuteNonQuery();
            }

            Status.SetText("Assigning derived categories... [3/4]");
            ApplyAutoCategories();
            Status.SetText("Assigning derived categories... [4/4]");
            CreatePhotoCategoryTrigger();

            Status.PopText();
            Status.Busy = false;
        }
    }
}
