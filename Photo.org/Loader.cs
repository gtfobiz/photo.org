using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;

namespace Photo.org
{
    internal static class Loader
    {
        private static Thread m_ThumbnailLoader = null;

        internal static void LoadThumbnails()
        {
            m_ThumbnailLoader = new Thread(new ThreadStart(ThumbnailLoaderProc));
            m_ThumbnailLoader.Start();
        }

        private static void ThumbnailLoaderProc()
        {
            Photo photo = null;
            Image thumbnail = null;
            Guid worklistId = Worklist.GetWorklistId();

            while (true)
            {
                if ((photo = Worklist.GetWork(worklistId)) == null)
                    return;
                
                thumbnail = photo.LoadThumbnail();
                if (thumbnail == null)
                    if (photo.IsVideo)
                        thumbnail = photo.CreateThumbnailFromVideo();
                    else
                        thumbnail = photo.CreateThumbnail();

                Worklist.ThumbnailLoaded(photo, photo.LoadThumbnail());
            }            
        }
    }
}
