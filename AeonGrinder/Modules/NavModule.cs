using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;

namespace AeonGrinder.Modules
{
    using Configs;

    internal class NavModule : CoreHelper
    {
        private Host Host;
        private Settings settings;
        private GpsModule gps;

        private List<string> ignorePoints = new List<string>();

        public bool IsGpsZone { get; set; }

        public NavModule(Host host, GpsModule gps, Settings settings) : base(host)
        {
            Host = host;
            this.settings = settings;
            this.gps = gps;
        }

        
        public RoundZone FightZone { get; set; }

        public bool InFightRadius(Creature obj = null) 
            => FightZone.ObjInZone((obj == null) ? Host.me : obj);

        public double FightCenterDist() 
            => (FightZone != null) ? Host.me.dist(FightZone.X, FightZone.Y) : 0;

        public double FightRadius
        {
            get { return (FightZone != null) ? FightZone.radius : 0; }
        }


        public bool BuildZone()
        {
            if (settings.MapName != string.Empty && gps.Load(settings.MapName))
            {
                if (GetFightPoints().Count < 1)
                {
                    Log("Missing gps points: (Fight)");

                    return false;
                }

                var point = gps.GetNearestPoint();

                if (Host.dist(point.x, point.y, point.z) > 100)
                {
                    Log("Gps zone map too far away!");

                    return false;
                }

                IsGpsZone = true;
            }
            else
            {
                FightZone = new RoundZone(Host.me.X, Host.me.Y, settings.FightRadius);
            }

            return true;
        }

        public void GetFightZone()
        {
            var point = GetNearestFightPoint(true);

            if (point == null)
            {
                ignorePoints.Clear();
                point = GetNearestFightPoint();
            }

            if (point != null)
            {
                FightZone = new RoundZone(point.x, point.y, point.radius);
            }
        }

        public void CheckFightZone()
        {
            if (FightZone == null) GetFightZone();
        }

        public GpsPoint GetFightPoint()
        {
            var zone = new RoundZone(FightZone.X, FightZone.Y, 1);

            return GetFightPoints().Where(p => zone.PointInZone(p.x, p.y)).FirstOrDefault();
        }

        public void FlagFightZone()
        {
            var point = GetFightPoint();

            if (point != null)
            {
                ignorePoints.Add(point.name);
                FightZone = null;
            }
        }


        private List<GpsPoint> GetFightPoints() => gps.GetPointsByName("Fight");

        private GpsPoint GetNearestFightPoint(bool filter = false)
        {
            var points = GetFightPoints().OrderBy(p => Host.dist(p.x, p.y, p.z));

            if (!filter)
            {
                return points.FirstOrDefault();
            }
            else
            {
                return points.Where(p => !ignorePoints.Contains(p.name)).FirstOrDefault();
            }
        }
    }
}
