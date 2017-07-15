using System;
using System.Linq;
using ArcheBot.Bot.Classes;

namespace CombatMaster.Modules
{
    using Data;

    public sealed partial class BaseModule
    {
        private void LootMobs()
        {
            while (token.IsAlive() && lootBag.Count > 0)
            {
                if (IsAnyAggro())
                    break;


                var bodies = Host.getCreatures().Where
                    (c => lootBag.Contains(c.GetHashCode()) && Host.dist(c) < 18).OrderBy(c => Host.dist(c));

                if (bodies.Count() == 0)
                {
                    lootBag.Clear();
                    break;
                }


                var body = bodies.First();
                
                if (Host.me.isSwim)
                {
                    if (!Host.ComeTo(body))
                        continue;
                }
                else
                {
                    var point = GetRandZonePoint(new RoundZone(body.X, body.Y, 2.8), Host.angle(Host.me), 60);

                    if (!Host.MoveTo(point[0], point[1], 0))
                        continue;
                }

                Utils.Delay(new int[] { 350, 650 }, new int[] { 550, 850 }, token);


                var result = Host.PickupAllDrop(body);
                var err = Host.GetLastError();

                if (result || (!result && (err == LastError.ResponseFalse || err == LastError.InvalidTarget)))
                {
                    // Remove from loots
                    lootBag.Remove(body.GetHashCode());
                }
            }
        }

        private void CleanItems()
        {
            if (settings.CleanItems.Count < 1)
                return;

            var junk = Host.getAllInvItems().Where(i => settings.CleanItems.Contains(i.name));

            if (junk.Count() < 1)
                return;


            foreach (var item in junk)
            {
                if (!token.IsAlive() || !UnderAttack())
                    return;

                if (item.DeleteItem())
                {
                    Utils.Delay(650, 1450, token);
                }
            }
        }

        private void ProcessItems()
        {
            if (settings.ProcessItems.Count < 1)
                return;

            var items = Host.getAllInvItems().Where
                (i => settings.ProcessItems.Contains(i.name) && Host.itemCooldown(i) == 0);

            if (items.Count() < 1 || GetInventoryFreeSpace() < 2)
                return;


            foreach (var i in items)
            {
                if (UnderAttack())
                    break;

                while (token.IsAlive() && !UnderAttack() && i.count > 0)
                {
                    if (i.UseItem())
                    {
                        Utils.Delay(350, 650, token);
                    }
                    else
                    {
                        if (Host.GetLastError() == LastError.ResponseFalse || GetInventoryFreeSpace() < 2)
                        {
                            return;
                        }
                    }
                }
            }
        }

        private void ManageFams()
        {
            if (memory.IsLocked("ManageFams"))
                return;

            var fam = Host.getMount();

            if (fam != null)
                return;


            var item = Host.getInvItem(settings.FamiliarName);

            if (item == null)
                return;


            if (item.UseItem())
            {
                Utils.Delay(450, 850, token);
            }
            else
            {
                if (Host.GetLastError() == LastError.ResponseTimeout)
                {
                    memory.Lock("ManageFams", Utils.Rand(75 * 1000, 140 * 1000));
                }
            }
        }

        private bool MakeRevival()
        {
            int secs = Utils.Rand(4500, 12500);

            Log("Resurrecting in: " + (secs / 1000) + " seconds");
            Utils.Delay(secs, token);


            while (token.IsAlive() && !Host.me.isAlive() && !Host.ResToRespoint())
            {
                Utils.Delay(1650, 3250, token);
            }

            while (!Host.IsGameReady()) Utils.Delay(50, token);


            return Host.me.isAlive();
        }

        private void CombatBoosts()
        {
            if (settings.CombatBoosts.Count < 1)
                return;


            var items = Host.getAllInvItems().Where
                (i => settings.CombatBoosts.Contains(i.name) && Host.itemCooldown(i) == 0);

            if (items.Count() < 1)
                return;


            foreach (var item in items)
            {
                if (UnderAttack())
                    break;

                if (BuffExists(GetBuffByItem(item.id)) || AnyBuffContains(item.name))
                    continue;

                if (item.UseItem())
                {
                    Utils.Delay(450, 850, token);
                }
            }
        }

        private bool UseHealsPots()
        {
            if (settings.HpPotions.Count < 1)
                return false;


            var items = Host.getAllInvItems().Where
                (i => settings.HpPotions.Contains(i.name) && Host.itemCooldown(i) == 0);

            if (items.Count() < 1)
                return false;


            var potion = items.First();

            return (potion != null && potion.UseItem());
        }

        private bool RecoverHitpoints()
        {
            var items = Host.getAllInvItems().Where
                (i => settings.HpRecoverItems.Contains(i.name) && Host.itemCooldown(i) == 0);

            if (items.Count() < 1)
                return false;

            var item = items.FirstOrDefault();


            return (item != null && item.UseItem());
        }

        private bool RecoverMana()
        {
            if (settings.UseMeditate)
            {
                uint s1 = Abilities.Auramancy.Meditate;

                if (SkillExists(s1) && Host.skillCooldown(s1) == 0)
                {
                    return Host.UseSkill(s1);
                }
            }


            var items = Host.getAllInvItems().Where
                (i => settings.ManaRecoverItems.Contains(i.name) && Host.itemCooldown(i) == 0);

            if (items.Count() < 1)
                return false;

            var item = items.FirstOrDefault();


            return (item != null && item.UseItem());
        }

        public void RecoverStats()
        {
            int resumeHpp = Math.Min(100, settings.MinHitpoints + Utils.Rand(15, 25));
            int resumeMpp = Math.Min(100, settings.MinMana + Utils.Rand(15, 25));

            bool hppRecover = Host.me.hpp <= settings.MinHitpoints;
            bool mppRecover = Host.me.mpp <= settings.MinMana;
            bool ready1 = false;
            bool ready2 = false;


            while (token.IsAlive() && !UnderAttack() && !(ready1 && ready2))
            {
                if (hppRecover && (Host.me.hpp < resumeHpp))
                {
                    if (!memory.IsLocked("HpRecover") && (resumeHpp - Host.me.hpp) >= 8)
                    {
                        RecoverHitpoints();

                        memory.Lock("HpRecover", 8 * 1000);
                    }
                }
                else
                {
                    ready1 = true;
                }


                if (UnderAttack())
                    return;

                if (mppRecover && (Host.me.mpp < resumeMpp))
                {
                    if (!memory.IsLocked("ManaRecover") && (resumeMpp - Host.me.mpp) >= 8)
                    {
                        RecoverMana();

                        memory.Lock("ManaRecover", 8 * 1000);
                    }
                }
                else
                {
                    ready2 = true;
                }


                if (UnderAttack())
                    return;

                if (settings.UseInstruments && !memory.IsLocked("Instrument"))
                {
                    if ((hppRecover && IsWeaponCatEquiped(80)) || (mppRecover && IsWeaponCatEquiped(81)))
                    {
                        PlayInstrument();

                        memory.Lock("Instrument", Utils.Rand(14 * 1000, 20 * 1000), Utils.Rand(1, 3));
                    }
                }


                Utils.Delay(650, 1250, token);
            }
        }

        public bool EscapeDeath()
        {
            uint s1 = Abilities.Shadowplay.Stealth;
            uint s2 = Abilities.Shadowplay.DropBack;
            uint s3 = Abilities.Witchcraft.PlayDead;

            if (SkillExists(s1))
            {
                if (SkillExists(s2) && Host.UseSkill(s2, false))
                {
                    Utils.Delay(350, 650, token);
                }

                if (Host.UseSkill(s1, false))
                {
                    Utils.Delay(1450, 1650, token);
                }

                if (!IsAnyAggro())
                    return true;
            }

            if (SkillExists(s3) && Host.UseSkill(s3))
            {
                Utils.Delay(1450, 1650, token);
            }


            return !IsAnyAggro();
        }

        private void PlayInstrument()
        {
            if (IsWeaponTypeEquiped(23))
                return;


            Host.UseSkill(14900);

            while (token.IsAlive() && Host.me.isCasting)
            {
                if (UnderAttack())
                {
                    Host.CancelSkill();
                    break;
                }

                Utils.Delay(50, token);
            }
        }
    }
}
