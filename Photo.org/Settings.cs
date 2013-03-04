using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Photo.org
{
    internal static class Settings
    {
        internal static string ImportPath = "";
        internal static bool CategoryFormSearchFromBegin = true;

        private static DataSet m_Settings = new DataSet("settings");        

        private static void CheckSetting(string setting)
        {
            if (m_Settings.Tables.Count == 0)
                m_Settings.Tables.Add("general");

            if (!m_Settings.Tables[0].Columns.Contains(setting))
                m_Settings.Tables[0].Columns.Add(setting);

            if (m_Settings.Tables[0].Rows.Count == 0)
                m_Settings.Tables[0].Rows.Add();
        }

        internal static string Get(string setting)
        {
            CheckSetting(setting);
            return m_Settings.Tables[0].Rows[0][setting].ToString();
        }

        internal static void Set(string setting, string value)
        {
            CheckSetting(setting);
            m_Settings.Tables[0].Rows[0][setting] = value;
        }

        /// <summary>
        /// Loads settings
        /// </summary>
        internal static void Load()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            try
            {
                if (System.IO.File.Exists(path + @"\Photo.org.settings.xml"))
                    m_Settings.ReadXml(path + @"\Photo.org.settings.xml");
            }
            catch
            {
            }
        }

        /// <summary>
        /// Saves current settings
        /// </summary>
        internal static void Save()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            m_Settings.WriteXml(path + @"\Photo.org.settings.xml");            
        }
    }
}
