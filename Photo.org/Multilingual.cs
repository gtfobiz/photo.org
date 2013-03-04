using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Photo.org
{
    internal static class Multilingual
    {
        private static DataSet m_Texts = new DataSet("multilingual");
        private static bool m_NeedsToBeSaved = false;

        internal static void Load()
        {
            if (m_NeedsToBeSaved)
                Save();

            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            try
            {
                if (System.IO.File.Exists(path + @"\Photo.org.multilingual.xml"))
                    m_Texts.ReadXml(path + @"\Photo.org.multilingual.xml");
            }
            catch
            {
                throw;
            }
        }

        internal static void Save()
        {
            if (!m_NeedsToBeSaved)
                return;

            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            m_Texts.WriteXml(path + @"\Photo.org.multilingual.xml");

            m_NeedsToBeSaved = false;
        }

        internal static string GetText(string section, string name, string text)
        {
            DataTable dt = null;

            if (m_Texts.Tables.Contains(section))
            {
                dt = m_Texts.Tables[section];
            }
            else
            {
                dt = m_Texts.Tables.Add(section);
                dt.Columns.Add("name");
                dt.Columns.Add("text");
            }

            DataRow[] rows = dt.Select("name = '" + name + "'");
            if (rows.Length > 0)
                return rows[0]["text"].ToString();

            DataRow dr = dt.NewRow();
            dr["name"] = name;
            dr["text"] = text;
            dt.Rows.Add(dr);

            m_NeedsToBeSaved = true;

            return text;
        }
    }
}
