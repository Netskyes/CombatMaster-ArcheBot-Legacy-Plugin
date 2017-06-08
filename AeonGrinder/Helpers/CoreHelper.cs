using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ArcheBot.Bot.Classes;

namespace AeonGrinder
{
    using Data;

    public abstract class CoreHelper
    {
        private Host Host;

        public CoreHelper(Host host)
        {
            Host = host;
        }


        public void Log(string text)
        {
            Host.Log(text);
        }

        public void Log(string text, Color color)
        {
            Host.Log(text, color);
        }


        #region Helpers

        public bool IsPatron()
        {
            return Host.getBuff(8000011) != null;
        }

        public bool IsPeaceZone()
        {
            return Host.getBuff(2149) != null;
        }

        public bool CanUpgradeLevel(uint profId)
        {
            int[] levels = new int[] 
            {
                10000, 20000, 30000, 40000,
                50000, 70000, 90000, 110000, 130000, 150000
            };

            var points = Host.me.getActabilities().Find(a => a.db.id == profId)?.points ?? 0;

            return (levels.Contains(points));
        }

        public bool UpgradeLevel(uint profId)
        {
            return Host.me.getActabilities().Find(a => a.db.id == profId)?.IncreaseLevel() ?? false;
        }

        public int GetProfLevel(int profId)
        {
            return Host.me.getActabilities().Find(a => a.db.id == profId)?.points ?? 0;
        }

        public bool BuffExists(uint buffId)
        {
            return Host.getBuff(buffId) != null;
        }

        public bool BuffExists(Creature obj, uint buffId)
        {
            return (Host.getBuff(obj, buffId) != null);
        }

        public bool AnyBuffExists(IEnumerable<uint> buffs)
        {
            return Host.me.getBuffs().Any(b => buffs.Contains(b.id));
        }

        public void CancelAnyBuffs(IEnumerable<uint> buffs)
        {
            var active = Host.me.getBuffs().Where(b => buffs.Contains(b.id));

            if (active.Count() > 0)
            {
                foreach (var b in active) b.CancelBuff();
            }
        }

        public uint GetBuffByItem(uint itemId)
        {
            return Host.sqlCore.sqlItems.FirstOrDefault(i => i.Value.id == itemId).Value?.useSkillId ?? 0;
        }

        public bool IsItemExists(uint id)
        {
            return (Host.getInvItem(id) != null);
        }

        public bool IsItemExists(uint[] ids)
        {
            return (Host.getAllInvItems().Where(i => ids.Contains(i.id)).Count() > 0);
        }

        public bool IsInventoryFull()
        {
            return (Host.inventoryItemsCount() == Host.maxInventoryItemsCount());
        }

        public int GetInventoryFreeSpace()
        {
            return (Host.maxInventoryItemsCount() - Host.inventoryItemsCount());
        }

        public bool IsQuestActive(uint questId)
        {
            return Host.getQuests().Any(q => q.id == questId);
        }
        
        public bool IsQuestComplete(uint questId)
        {
            return Host.getCompletedQuests().Any(q => q.id == questId);
        }

        public bool CanUseItem(Item item)
        {
            return (item != null && (Host.itemCooldown(item) == 0));
        }

        public bool CanUseItem(string itemName)
        {
            return CanUseItem(Host.getInvItem(itemName));
        }

        public bool IsWeaponTypeEquiped(int type)
        {
            return Host.me.getAllEquipedItems().Any(i => i.db.weaponTypeId == type);
        }

        public void CancelMove() => Host.CancelMoveTo();

        #endregion

        #region Combat Helpers

        public bool UnderAttack()
        {
            return IsAnyAggro() || InFight();
        }

        public bool IsUnderAttack(Creature obj)
        {
            return (Host.getAggroMobsCount(obj) > 0) || obj.inFight;
        }

        public bool IsAnyAggro()
        {
            return Host.getAggroMobsCount() > 0;
        }

        public bool UnderAggroBy(Creature obj)
        {
            return Host.getAggroMobs().Any(c => c == obj);
        }

        public bool UnderAggroBy(Creature obj, Creature objAggro) => Host.getAggroMobs(obj).Any(c => c == objAggro);

        public bool InFight()
        {
            return Host.me.inFight;
        }

        public List<Creature> GetTargetAggro(Creature obj) => Host.getCreatures().Where(c => c.target == obj).ToList();

        public bool IsCasting()
        {
            return Host.me.isCasting || Host.me.isGlobalCooldown;
        }

        public bool CanCastSkill(Skill skill)
        {
            return (skill != null && !Host.me.isGlobalCooldown && (Host.skillCooldown(skill) == 0));
        }

        public bool CanCastSkill(string skillName)
        {
            return CanCastSkill(Host.getSkill(skillName));
        }

        public bool IsAttackable(Creature obj)
        {
            return (obj.isAlive() && Host.isAttackable(obj) && !IsAttackImmune(obj) && !BuffExists(obj, 550));
        }

        public bool IsAttackImmune(Creature obj)
        {
            return (Host.isMeleeImmune(obj) || Host.isRangedImmune(obj) || Host.isSpellImmune(obj));
        }

        public bool IsDisabled(Creature obj)
        {
            return (Host.isKnockedDown(obj) || Host.isSleep(obj) || Host.isStun(obj));
        }

        public bool IsSilenced(Creature obj)
        {
            return (Host.isSilense(obj));
        }

        public bool IsSlowDown(Creature obj)
        {
            return (Host.isCrippled(obj) || Host.isRoot(obj));
        }

        #endregion

        #region Navigation

        public bool InNavMesh(SpawnObject obj) 
            => obj != null ? InNavMesh(obj.X, obj.Y, obj.Z) : false;

        public bool InNavMesh(double x, double y, double z)
        {
            return Host.IsInsideNavMesh(x, y, z);
        }

        public IEnumerable<Point3D> GetNavPath(double sX, double sY, double sZ, double eX, double eY, double eZ)
        {
            var path = Host.GetNavPath(sX, sY, sZ, eX, eY, eZ);

            if (path.Count() < 1)
                yield break;


            for (int i = 0; i < path.Length / 3; i++)
            {
                var coords = Array.ConvertAll(path.Skip(i * 3).Take(3).ToArray(), x => (double)x);

                yield return new Point3D(coords);
            }
        }

        /// <summary>
        /// From me to spawn object.
        /// </summary>
        public double GetNavDist(SpawnObject obj)
            => obj != null ? GetNavDist(obj.X, obj.Y, obj.Z) : 0;

        /// <summary>
        /// From me to coordinates.
        /// </summary>
        public double GetNavDist(double x, double y, double z)
            => GetNavDist(Host.me.X, Host.me.Y, Host.me.Z, x, y, z);

        public double GetNavDist(double sX, double sY, double sZ, double eX, double eY, double eZ)
        {
            var path = GetNavPath(sX, sY, sZ, eX, eY, eZ).ToArray();

            if (path.Length < 1)
                return 0;


            Point3D temp = null;
            double dist = 0;

            for (int i = 0; i < path.Count(); i++)
            {
                if (temp != null)
                {
                    dist += Math.Sqrt(Math.Pow((path[i].X - temp.X), 2.0) + Math.Pow((path[i].Y - temp.Y), 2.0) + Math.Pow((path[i].Z - temp.Z), 2.0));
                }

                temp = path[i];
            }

            return dist;
        }

        #endregion

        #region Math

        public double AngleToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public double[] GetRayCast(double dist)
        {
            int angle = Host.angle(Host.me);

            double x = Host.me.X,
                   y = Host.me.Y,
                   z = Host.me.Z;


            x = x + dist * Math.Cos(AngleToRadians(angle));
            y = y + dist * Math.Sin(AngleToRadians(angle));
            z = Host.getZFromHeightMap(x, y);


            return new double[] { x, y, z };
        }

        public bool TurnToAngle(double nAngle)
        {
            double angle = Host.angle(Host.me);
            return Host.Turn(AngleToRadians((360 - angle) + nAngle), true);
        }

        public bool TurnToCoords(double x, double y)
        {
            return Host.Turn(-AngleToRadians(Host.angle(Host.me, x, y)));
        }

        public bool IsCoordsBehind(double x, double y)
        {
            double ang = (Host.angle(Host.me, x, y) / 90);

            return (ang >= 1 && ang < 3);
        }

        public bool IsFacingAngle(double x, double y, int angle)
        {
            double ang = Host.angle(Host.me, x, y);

            return Math.Abs(((int)(ang / 180) * 360) - ang) < angle;
        }

        #endregion
    }
}
