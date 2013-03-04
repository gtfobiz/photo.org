using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace Photo.org
{
    internal static class Worklist
    {
        internal delegate void ThumbnailLoadedHandler(Guid photoId, Image thumbnail);
        internal static event ThumbnailLoadedHandler OnThumbnailLoaded;

        private static Guid m_WorklistId = Guid.Empty;
        private static readonly object m_Locker = new object();
        private static List<Photo> m_Worklist = new List<Photo>();

        internal static void SetWorkList(List<Photo> worklist)
        {          
            Clear();
            m_Worklist = worklist;
        }

        internal static void ThumbnailLoaded(Photo photo, Image thumbnail)
        {
            if (thumbnail == null)
                return; 

            lock (m_Locker)
            {
                if (OnThumbnailLoaded != null)
                    OnThumbnailLoaded(photo.Id, thumbnail);
            }
        }

        internal static Guid GetWorklistId()
        {
            return m_WorklistId;
        }

        internal static Photo GetWork(Guid worklistId)
        {
            if (worklistId != m_WorklistId)
                return null;

            Photo photo = null;

            lock (m_Locker)
            {
                if (m_Worklist.Count > 0)
                {                
                    photo = m_Worklist[0];
                    m_Worklist.Remove(photo);
                }                
            }

            return photo;
        }

        internal static void Clear()
        {
            m_WorklistId = Guid.NewGuid();
            m_Worklist.Clear();
        }
    }    
}
