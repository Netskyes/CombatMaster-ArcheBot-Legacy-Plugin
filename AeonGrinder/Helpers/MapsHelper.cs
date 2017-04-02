using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AeonGrinder
{
    using Data;
    using Properties;

    public static class MapsHelper
    {
        private static readonly IEnumerable<ZoneMap> maps = new List<ZoneMap>()
        {
            // Future use.
        };

        public static IEnumerable<ZoneMap> GetAll()
        {
            foreach (var map in maps.Concat(GetLocal()))
            {
                yield return map;
            }
        }

        public static IEnumerable<ZoneMap> GetLocal()
        {
            string[] maps = { };

            try
            {
                maps = Directory.GetFiles($"{Paths.ZoneMaps}", "*.db3");
            }
            catch
            {
            }

            foreach (var m in maps)
            {
                var name = Path.GetFileNameWithoutExtension(m);
                var temp = new ZoneMap(name, $"{name}.db3", $"{name}.ABMesh");

                yield return temp;
            }
        }

        public static ZoneMap GetMap(string name)
        {
            return GetAll().FirstOrDefault(m => m.Name == name) as ZoneMap;
        }
    }
}
