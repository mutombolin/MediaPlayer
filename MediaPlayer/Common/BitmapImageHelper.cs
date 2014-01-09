using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace MediaPlayer.Common
{
    public class BitmapImageHelper
    {
        public static BitmapImage NewBitmapImage(Uri uri)
        {
            BitmapImage bmp = null;
            try
            {
                bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CreateOptions |= BitmapCreateOptions.IgnoreColorProfile;
                bmp.UriSource = uri;
                bmp.EndInit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                ex,
                "Failed to create bitmap image"));

            }

            return bmp;
        }
    }
}
