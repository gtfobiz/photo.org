using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Photo.org
{
    internal static class Status
    {        
        internal static Component ActiveComponent = Component.Photos;
        internal static bool LabelEdit = false;
        internal static bool ReadOnly = false;
        internal static bool ShowHiddenPhotos = false;        

        private static MainForm m_MainForm = null;
        private static StatusStrip m_StatusStrip = new StatusStrip();
        private static ToolStripProgressBar m_ProgressBar = new ToolStripProgressBar();
        private static ToolStripStatusLabel m_StatusText = new ToolStripStatusLabel();
        private static bool m_InBusyStatus = false;
        private static Stack<string> m_TextStack = new Stack<string>();

        public static bool Busy
        {
            get { return m_InBusyStatus; }
            set
            {
                m_InBusyStatus = value;
                m_MainForm.Cursor = (m_InBusyStatus ? Cursors.WaitCursor : Cursors.Default);
                Menus.SetEnabled(!m_InBusyStatus);
            }
        }        

        internal static void Initialize(MainForm mainForm)
        {
            m_MainForm = mainForm;

            m_ProgressBar.Visible = false;
            m_StatusStrip.Items.Add(m_ProgressBar);

            //m_StatusText.Visible = true;
            //m_StatusText.Text = "testi";
            m_StatusStrip.Items.Add(m_StatusText);

            m_MainForm.Controls.Add(m_StatusStrip);
        }

        internal static void SetText(string text)
        {
            if (text == "")
            {                
                m_StatusText.Visible = false;
                m_StatusText.Text = "";
            }
            else
            {
                m_StatusText.Text = text;
                m_StatusText.Visible = true;
            }
            Application.DoEvents();
        }

        internal static void ShowProgress()
        {
            ShowProgress(0, 1, 0);
            Application.DoEvents();
        }

        /// <summary>
        /// Shows progress bar and changes mouse cursor to wait cursor
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="value"></param>
        internal static void ShowProgress(int minValue, int maxValue, int value)
        {
            m_ProgressBar.Minimum = minValue;
            m_ProgressBar.Maximum = maxValue;
            m_ProgressBar.Value = value;
            m_ProgressBar.Visible = true;
            Status.Busy = true;
        }

        internal static void SetMaxValue(int maxValue)
        {
            m_ProgressBar.Maximum = maxValue;
        }

        /// <summary>
        /// Sets progress bar progress value
        /// </summary>
        /// <param name="value"></param>
        internal static void SetProgress(int value)
        {
            m_ProgressBar.Value = value;
        }

        /// <summary>
        /// Hides progress bar and changes mouse cursor to default cursor
        /// </summary>
        internal static void HideProgress()
        {
            m_ProgressBar.Visible = false;
            Status.Busy = false;
        }

        internal static void PushText()
        {
            m_TextStack.Push(m_StatusText.Text);
        }

        internal static void PopText()
        {
            m_StatusText.Text = m_TextStack.Pop();
        }
    }
}
