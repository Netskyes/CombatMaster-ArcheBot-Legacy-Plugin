using System;
using System.IO;

namespace CombatMaster
{
    public static class Paths
    {
        /// <summary>
        /// Plugins/AeonGrinder
        /// </summary>
        public static string Folder
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Plugins\CombatMaster"); }
        }

        /// <summary>
        /// Plugins/AeonGrinder/
        /// </summary>
        public static string Plugin
        {
            get { return Folder + @"\"; }
        }

        public static string Logs
        {
            get { return Path.Combine(Plugin, @"Logs\"); }
        }

        public static string Settings
        {
            get { return Path.Combine(Plugin, @"Settings\"); }
        }

        public static string Templates
        {
            get { return Path.Combine(Plugin, @"Templates\"); }
        }

        public static string ZoneMaps
        {
            get { return Path.Combine(Plugin, @"ZoneMaps\"); }
        }

        public static string MeshMaps
        {
            get { return Path.Combine(Plugin, @"ZoneMaps\MeshMaps\"); }
        }

        public static string[] Structure = { "Logs", "Settings", "Templates", "ZoneMaps", @"ZoneMaps\MeshMaps" };


        public static void Validate()
        {
            if (!Directory.Exists(Folder))
            {
                try
                {
                    Directory.CreateDirectory(Folder);
                }
                catch
                {
                }
            }

            foreach (string name in Structure)
            {
                string path = Path.Combine(Plugin, name);

                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
