using System.Collections.Generic;
using System.IO;

namespace NeosPlusLauncher.ViewModels
{
    public static class NeosPathHelper
    {
        public static string[] GetNeosPaths()
        {
            string[] defaultPaths =
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\NeosVR\",
                @"C:\Neos\app\",
                @"/.steam/steam/steamapps/common/NeosVR/",
                @"/mnt/LocalDisk/SteamLibrary/steamapps/common/NeosVR/"
            };

            List<string> existingPaths = new List<string>();
            foreach (string path in defaultPaths)
            {
                if (Directory.Exists(path))
                {
                    existingPaths.Add(path);
                }
            }

            // Check if CustomInstallDir is set in the configuration
            Config config = Config.LoadConfig();
            if (!string.IsNullOrEmpty(config.CustomInstallDir) && Directory.Exists(config.CustomInstallDir))
            {
                existingPaths.Insert(0, config.CustomInstallDir);
            }

            return existingPaths.ToArray();
        }
    }
}
