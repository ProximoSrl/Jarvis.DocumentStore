using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.VideoThumbnails
{
    public static class Helper
    {
        public static String GetExecutableLocation()
        {
            String appConfig = ConfigurationManager.AppSettings["vlc_location"] ?? "";
            var possibleLocations = new[]
            {
                appConfig + "\\vlc.exe",
                @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe",
                @"C:\Program Files\VideoLAN\VLC\vlc.exe"
            };

            return possibleLocations.FirstOrDefault(l => File.Exists(l));
        }
    }
}
