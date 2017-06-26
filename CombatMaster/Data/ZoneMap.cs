using System.IO;

namespace CombatMaster.Data
{
    using Enums;

    public sealed class ZoneMap
    {
        public string Name { get; set; }
        public MapUseType MapUseType { get; set; }

        #region Props

        private string dbName;
        private string meshName;
        private byte[] db;
        private byte[] mesh;

        #endregion

        public ZoneMap(string name, byte[] db, byte[] mesh)
        {
            Name = name;
            MapUseType = MapUseType.Internal;
            this.db = db;
            this.mesh = mesh;
        }

        public ZoneMap(string name, string dbName, string meshName)
        {
            Name = name;
            MapUseType = MapUseType.Local;
            this.dbName = dbName;
            this.meshName = meshName;
        }

        public bool MeshExists()
        {
            if (MapUseType == MapUseType.Local)
            {
                return File.Exists(Path.Combine(Paths.MeshMaps, meshName));
            }

            else if (MapUseType == MapUseType.Internal)
            {
                return mesh != null;
            }

            return false;
        }


        public byte[] GetByteMap()
        {
            return db;
        }

        public byte[] GetByteMesh()
        {
            return mesh;
        }

        public string GetMapPath()
        {
            return Path.Combine(Paths.ZoneMaps, dbName);
        }

        public string GetMeshPath()
        {
            return Path.Combine(Paths.MeshMaps, meshName);
        }
    }
}
