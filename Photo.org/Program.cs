using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;

namespace Photo.org
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Common.ParseCommandLine(args);
                Application.Run(new MainForm());
            }
            //catch (Exception ex)
            //{
            //    Exception e = ex;

            //    try
            //    {
            //        StringBuilder stringBuilder = new StringBuilder();

            //        stringBuilder.AppendLine("-----------------------------------------------------");
            //        stringBuilder.AppendLine(DateTime.Now.ToString());
            //        stringBuilder.AppendLine();

            //        while (ex != null)
            //        {
            //            stringBuilder.AppendLine(ex.Message);
            //            stringBuilder.AppendLine(ex.StackTrace);

            //            ex = ex.InnerException;
            //        }

            //        System.Diagnostics.Debug.Assert(false, stringBuilder.ToString());

            //        System.IO.File.AppendAllText("photo.org.error.log", stringBuilder.ToString());
            //    }
            //    catch
            //    {
            //    }

            //    throw e;
            //}
        }
    }
}
