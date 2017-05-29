using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;
using System.Diagnostics;

namespace AeonGrinder.Modules
{
    using Data;
    using Enums;

    public sealed partial class BaseModule : CoreHelper
    {
        private Statistics stats;

        private State state;
        private Creature target;
        private DateTime? seekTime;

        private List<string> rotation;
        private Dictionary<string, Combos> combos;
        private int sequence;
        private Queue<string> skillLoader;

        private List<int> toLoot;
        private List<string> checkPoints = new List<string>();

        private bool isMoving;
        private bool isFighting;

        private RoundZone fightZone;
        
       
        /// <summary>
        /// Initialize runtime.
        /// </summary>
        private bool Initialize()
        {
            if (!BuildFightZone())
                return false;
            
            if (template.Rotation.Count < 1)
            {
                Log("Please build your routine before starting!");

                return false;
            }
            else
            {
                BuildRoutine();
            }

            if (stats == null || UI.StatsReset())
                stats = new Statistics(UI);
            

            // Events
            HookGameEvents();

            // Resets
            SetState(State.Check);
            seekTime = null;
            toLoot = new List<int>();
            skillLoader = new Queue<string>();
            
            
            return true;
        }

        private void StopRequest()
        {
            Stop();

            switch (settings.FinalAction)
            {
                case "TerminateClient":
                    Host.TerminateGameClient();
                    break;

                case "ToSelectionScreen":
                    Host.LeaveWorldToCharacterSelect();
                    break;
            }

            if (settings.RunPlugin && settings.RunPluginName != string.Empty)
            {
                Host.RunPlugin(settings.RunPluginName);
            }
        }


        private bool BuildFightZone()
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
                    Log("Gps zone map is too far away!");

                    return false;
                }
            }
            else
            {
                fightZone = new RoundZone(Host.me.X, Host.me.Y, settings.FightRadius);
            }

            return true;
        }

        private void BuildRoutine()
        {
            sequence = 0;

            rotation = template.CombatBuffs;
            rotation = rotation.Concat(template.Rotation).ToList();

            combos = template.Combos.ToDictionary(c => c.Name, c => c);
        }



        private void Execute()
        {
            CriticalCheck();

            switch (state)
            {
                case State.Check:
#if DEBUG
                    Host.Log("State: Check");
#endif              
                    Check();
                    break;

                case State.Search:
#if DEBUG
                    Host.Log("State: Search");
#endif              
                    Search();
                    break;

                case State.Move:
#if DEBUG
                    Host.Log("State: Move");
#endif              
                    Move();
                    break;

                case State.Prepare:
#if DEBUG
                    Host.Log("State: Prepare");
#endif              
                    Prepare();
                    break;

                case State.Fight:
#if DEBUG
                    //Host.Log("State: Fight");
#endif              
                    Fight();
                    break;

                case State.Analyze:
#if DEBUG
                    Host.Log("State: Analyze");
#endif              
                    Analyze();
                    break;
            }
        }


        private void CriticalCheck()
        {
            if (!Host.me.isAlive())
            {
                stats.Deaths++;

                if (MakeRevival())
                {
                    SetState(State.Check);

                    return;
                }
            }

            if (Host.me.hpp < 20 && EscapeDeath())
            {
                SetState(State.Check);

                return;
            }
        }

        private void Check()
        {
            if (!UnderAttack() && IsNeedsResting())
            {
                RecoverStats();

                if (!token.IsAlive())
                    return;
            }


            if (!ZoneExists())
            {
                GenerateFightZone();
            }

            if (!UnderAttack() && !InZoneRadius())
            {
                ComeToCenter();

                if (!token.IsAlive())
                    return;
            }
            

            if (!UnderAttack())
            {
                if (settings.LevelFamiliars)
                {
                    ManageFams();
                }

                if (IsCleanEnabled() && JunkItemsCount() >= Utils.RandomNum(1, 4))
                {
                    Log("Cleaning inventory...");
                    CleanItems();
                }

                // ...
            }


            SetState(State.Search);
        }

        private void Search()
        {
            if (UnderAttack())
            {
                var mobs = Host.getAggroMobs();
                var fams = Host.getMounts();

                if (mobs.Count < 1 && fams.Count > 0)
                {
                    foreach (var f in fams)
                    {
                        mobs = mobs.Concat(GetTargetAggro(f)).ToList();
                    }
                }


                var mob = mobs.Where(m => IsAttackable(m)).OrderBy(m => Host.dist(m)).FirstOrDefault();

                if (mob != null)
                {
                    SetTarget(mob);

                    SetState(State.Fight);
                    return;
                }
            }


            GetTarget();
            
            if (target != null)
            {
                // Reset time
                seekTime = null;

                SetState((Host.dist(target) > 20 ? State.Move : State.Prepare));
                return;
            }
            

            if (ZoneRadius >= 80 && ZoneCenterDist() > (ZoneRadius / 2))
            {
                ComeToCenter(Utils.RandomDouble(4, 39));

                return;
            }
            

            if (gps.IsLoaded)
            {
                if (seekTime == null)
                    seekTime = DateTime.Now;

                if ((DateTime.Now - seekTime).Value.TotalSeconds >= 18)
                {
                    seekTime = null;
                    FlagFightZone();

                    SetState(State.Check);
                }
            }
        }

        private void Move()
        {
            if (ComeToTarget(Utils.RandomNum(14, 18)))
            {
                Utils.Delay(new int[] { 225, 450 }, new int[] { 525, 750 }, new int[] { 825, 1050 }, token);

                SetState(State.Prepare);
            }
            else
            {
                SetState(State.Search);
            }
        }

        private void Prepare()
        {
            BoostUp();

            SetState(State.Fight);
        }

        private void Fight()
        {
            if (!isFighting)
            {
                Task.Run(() => FightWatch(), token);

                isFighting = true;
            }


            if (IsCasting())
                return;

            if (target != null && IsAttackable(target))
            {
                if (Host.me.target != target)
                {
                    if (Utils.RandomNum(0, 2) == 0) Host.SetTarget(target);
                }

                if (!IsDisabled(Host.me) && !IsSilenced(Host.me))
                {
                    if (Host.me.mpp > 5)
                    {
                        DoRoutine();
                    }
                    else
                    {
                        DoBasicAttack();
                    }
                }
                else
                {
                    // Anti CC skillz
                }

                return;
            }

            ResetLoader();
            Host.CancelSkill();
            isFighting = false;
            
            Utils.Delay(450, 650, token);


            SetState(State.Analyze);
        }

        private void Analyze()
        {
            if (!IsAnyAggro() && settings.LootTargets && toLoot.Count > 0)
            {
                if (!IsInventoryFull())
                {
                    Utils.Delay(new int[] { 250, 450 }, new int[] { 550, 750 }, new int[] { 850, 1050 }, token);

                    LootMobs();
                }
                else
                {
                    Log("Inventory full, cannot loot.");
                }
            }


            SetState(State.Check);
        }



        private void DoRoutine()
        {
            var name = "";
            var isLoaded = false;

            if (skillLoader.Count < 1)
            {
                name = rotation[sequence];
            }
            else
            {
                name = skillLoader.Peek();
                isLoaded = true;
            }


            var skill = Host.getSkill(name);

            if (skill == null || (!isLoaded && !CanCastSkill(skill)))
            {
                PushSequence();
                return;
            }

            var isBoosts = template.CombatBuffs.Contains(name);


            bool isCombo = false;
            List<string> combo = null;

            if (!isLoaded && !isBoosts && combos.ContainsKey(name))
            {
                var isValid = combos[name].Conditions.All(c => CheckCondition(c));

                if (isValid)
                {
                    combo = combos[name].Skills;

                    isCombo = (combo != null && combo.Count > 0 && !combo.Any(s => (Host.getSkill(s) == null) || (Host.skillCooldown(s) > 0)));
                }
            }


            if (UseSkill(skill, isBoosts, !isBoosts))
            {
                if (isLoaded)
                {
                    skillLoader.Dequeue();
                }
                else if (isCombo)
                {
                    LoadCombo(name);
                }


                Utils.Delay(new int[] { 50, 100 }, new int[] { 100, 150 }, new int[] { 150, 200 }, token);
            }
            else
            {
                //Log("Couldn't use: " + skill.name + " / Reason: " + Host.GetLastError());

                if (Host.GetLastError() == LastError.ActionNotAllowed)
                    return;
            }


            if (!isLoaded)
            {
                PushSequence();
            }
        }

        private void PushSequence()
        {
            sequence = (sequence < (rotation.Count - 1)) ? sequence + 1 : 0;
        }

        private void LoadCombo(string name)
        {
            if (!combos.ContainsKey(name))
                return;

            foreach (string combo in combos[name].Skills) skillLoader.Enqueue(combo);
        }

        private void ResetLoader()
        {
            sequence = 0;
            skillLoader.Clear();
        }


        private void DoBasicAttack()
        {
            if (Host.dist(target) <= 2)
            {
                Host.UseSkill(3);
            }
            else
            {
                Host.UseSkill(16064);
            }
        }

        private void BoostUp()
        {
            if (!AnyBoostExists())
                return;


            foreach (var b in template.BoostingBuffs)
            {
                if (UnderAttack())
                    break;


                var skill = Host.getSkill(b);
                
                if (skill == null || IsAnyBuffExists(SkillHelper.GetProdsBuffs(skill.id)))
                    continue;


                var result = Host.UseSkill(b, false, true);
                
                if (result)
                {
                    Utils.Delay(450, 850, token);
                }
            }
        }



        private bool UseSkill(Skill skill, bool selfTarget = false, bool autoCome = true)
        {
            while (IsCasting())
            {
                Utils.Delay(50, token);
            }

            bool isLocTarget = (skill.TargetType() == TargetType.Location);


            if (autoCome && !selfTarget)
            {
                double dist = Host.me.calcSkillMaxRange(skill.id);

                if (dist == 0 && skill.SelectType() == SelectType.None && skill.CastType() == CastType.Magic)
                {
                    // Damage area radius
                    dist = (skill.AreaRadius() - Utils.RandomDouble(0.5, 1.2));
                }


                if (dist != 0)
                {
                    double comeDist = (dist <= 4)
                        ? Utils.RandomDouble(0.8, 2) : dist - 1.5;

                    if ((Host.dist(target) > dist) && !ComeToTarget(comeDist))
                        return false;
                }
            }


            if (Host.me.target != target)
                Host.SetTarget(target);


            bool result = false;

            if (!isLocTarget)
            {
                result = skill.UseSkill(false, selfTarget);
            }
            else
            {
                result = Host.UseSkill(skill.id, target.X, target.Y, target.Z, false);
            }


            if (Host.GetLastError() == LastError.NoLineOfSight)
            {
                // Do something about it.
            }


            while(IsCasting())
            {
                Utils.Delay(50, token);
            }


            return result;
        }

        private void FightWatch()
        {
            while (isFighting && token.IsAlive())
            {
                try
                {
                    if (Host.dist(target) <= 8)
                    {
                        Host.TurnDirectly(target);
                    }
                }
                catch
                {
                }

                Utils.Delay(50, token);
            }
        }

        
        private void GetTarget() => SetTarget(FindTarget());

        private void SetTarget(Creature obj) 
            => target = obj;

        private Creature FindTarget()
        {
            var targets = Host.getCreatures().Where(c => InZoneRadius(c) && IsAttackable(c) && !IsUnderAttack(c));

            if (settings.Targets.Count > 0)
            {
                targets = targets.Where(c => settings.Targets.Contains(c.name));
            }

            if (targets.Count() < 1)
                return null;


            return targets.OrderBy(c => Host.dist(c)).First();
        }

        private bool ComeToTarget(double dist)
        {
            Func<bool> moveEval = () => (IsDisabled(Host.me) || (IsAnyAggro() && !UnderAggroBy(target)) || (!InFight() && IsUnderAttack(target)));

            isMoving = true;
            MoveUnless(moveEval);

            bool result = Host.ComeTo(target, dist, dist + Utils.RandomDouble(0.6, 1.1));
            isMoving = false;

            return result;
        }

        private void LootMobs()
        {
            while (toLoot.Count > 0 && token.IsAlive())
            {
                if (IsAnyAggro())
                    break;


                var bodies = Host.getCreatures().Where
                    (c => toLoot.Contains(c.GetHashCode()) && Host.dist(c) < 18).OrderBy(c => Host.dist(c));

                if (bodies.Count() == 0)
                {
                    toLoot.Clear();
                    break;
                }

                var body = bodies.First();


                if (!Host.MoveTo(body))
                    continue;

                Utils.Delay(250, 450, token);


                var result = Host.PickupAllDrop(body);
                var err = Host.GetLastError();

                if (result || (!result && (err == LastError.ResponseFalse || err == LastError.InvalidTarget)))
                {
                    // Remove from loots
                    toLoot.Remove(body.GetHashCode());
                }
            }
        }

        private void CleanItems()
        {
            Func<Item> getJunk = () => Host.getAllInvItems().Find(i => settings.CleanItems.Contains(i.name));

            if (getJunk() == null)
                return;


            while (token.IsAlive() && !UnderAttack())
            {
                bool result = (getJunk()?.DeleteItem() ?? false);

                if (result)
                {
                    Utils.Delay(850, 1650, token);
                }
                else
                {
                    break;
                }
            }
        }

        private void ManageFams()
        {
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
                    // Add cooldown
                }
            }
        }

        private bool MakeRevival()
        {
            int secs = Utils.RandomNum(4500, 12500);

            Log("Resurrecting in: " + (secs / 1000) + " seconds");
            Utils.Delay(secs, token);

            bool result = Host.ResToRespoint();


            if (result)
            {
                Utils.Delay(850, token);

                while (!Host.IsGameReady()) Utils.Delay(50, token);
            }

            return result;
        }

        private bool EscapeDeath()
        {
            uint s1 = Abilities.Witchcraft.PlayDead;
            uint s2 = Abilities.Shadowplay.Stealth;
            bool success = false;

            if (SkillExists(s1) && Host.skillCooldown(s1) == 0 && Host.UseSkill(s1))
            {
                success = true;
            }

            else if (SkillExists(s2) && Host.skillCooldown(s2) == 0 && Host.UseSkill(s2))
            {
                success = true;
            }


            if (success)
            {
                Utils.Delay(850, token);

                if (!UnderAttack()) SetState(State.Check);
            }

            return success;
        }

        private void RecoverStats()
        {
            DateTime? hppRecvTime = null;
            DateTime? mppRecvTime = null;

            int resumeHpp = Math.Min(100, settings.MinHitpoints + Utils.RandomNum(15, 25));
            int resumeMpp = Math.Min(100, settings.MinMana + Utils.RandomNum(15, 25));

            bool hppRecover = Host.me.hpp <= settings.MinHitpoints;
            bool mppRecover = Host.me.mpp <= settings.MinMana;
            bool ready1 = false;
            bool ready2 = false;


            while (token.IsAlive() && !UnderAttack() && !(ready1 && ready2))
            {
                if (hppRecover && (Host.me.hpp < resumeHpp))
                {
                    bool useItem = (hppRecvTime != null) 
                        ? (DateTime.Now - hppRecvTime.Value).TotalSeconds >= 10 && (resumeHpp - Host.me.hpp) >= 8 : true;

                    if (useItem)
                    {
                        RecoverHitpoints();
                        hppRecvTime = DateTime.Now;
                    }
                }
                else
                {
                    ready1 = true;
                }


                if (UnderAttack())
                    break;

                if (mppRecover && (Host.me.mpp < resumeMpp))
                {
                    bool useItem = (mppRecvTime != null) 
                        ? (DateTime.Now - mppRecvTime.Value).TotalSeconds >= 10 && (resumeMpp - Host.me.mpp) >= 8 : true;

                    if (useItem)
                    {
                        RecoverMana();
                        mppRecvTime = DateTime.Now;
                    }
                }
                else
                {
                    ready2 = true;
                }
                

                Utils.Delay(50, token);
            }
        }

        private bool RecoverHitpoints()
        {
            if (settings.UsePlayDead)
            {
                uint s1 = Abilities.Witchcraft.PlayDead;
                bool result = false;

                if (SkillExists(s1) && Host.skillCooldown(s1) == 0)
                {
                    result = Host.UseSkill(s1);
                    while (Host.me.isCasting && token.IsAlive()) Utils.Delay(50, token);

                    return result;
                }
            }


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
                bool result = false;

                if (SkillExists(s1) && Host.skillCooldown(s1) == 0)
                {
                    result = Host.UseSkill(s1);
                    while (Host.me.isCasting && token.IsAlive()) Utils.Delay(50, token);

                    return result;
                }
            }


            var items = Host.getAllInvItems().Where
                (i => settings.ManaRecoverItems.Contains(i.name) && Host.itemCooldown(i) == 0);

            if (items.Count() < 1)
                return false;

            var item = items.FirstOrDefault();


            return (item != null && item.UseItem());
        }


        private List<GpsPoint> GetFightPoints() => gps.GetPointsByName("Fight");
        private bool InZoneRadius(Creature obj = null) => fightZone.ObjInZone((obj == null) ? Host.me : obj);

        private GpsPoint GetCenterPoint()
        {
            var zone = new RoundZone(fightZone.X, fightZone.Y, 1);

            return GetFightPoints().Where(p => zone.PointInZone(p.x, p.y)).FirstOrDefault();
        }

        private void FlagFightZone()
        {
            var point = GetCenterPoint();

            if (point != null)
            {
                checkPoints.Add(point.name);
                fightZone = null;
            }
        }

        private GpsPoint GetNearestFightPoint(bool next = false)
        {
            var points = GetFightPoints().OrderBy(p => Host.dist(p.x, p.y, p.z));

            if (!next)
            {
                return points.FirstOrDefault();
            }
            else
            {
                return points.Where(p => !checkPoints.Contains(p.name)).FirstOrDefault();
            }
        }

        private void GenerateFightZone()
        {
            var point = GetNearestFightPoint(true);

            if (point == null)
            {
                checkPoints.Clear();
                point = GetNearestFightPoint();
            }
            
            if (point != null)
            {
                fightZone = new RoundZone(point.x, point.y, point.radius);
            }
        }

        private bool ComeToCenter(double dist = 0)
        {
            isMoving = true;
            bool result = false;

            if (gps.IsLoaded)
            {
                var point = GetCenterPoint();

                if (point != null)
                {
                    MoveUnless(() => UnderAttack() || (dist != 0 && Host.me.dist(point.x, point.y) <= dist));
                    result = gps.MoveToPoint(point);
                }
            }
            else
            {
                MoveUnless(() => UnderAttack() 
                    || (dist != 0 && Host.me.dist(fightZone.X, fightZone.Y) <= dist));

                result = Host.MoveTo(fightZone.X, fightZone.Y, 0);
            }

            isMoving = false;

            return result;
        }


        public bool CheckCondition(Condition cond)
        {
            int value = 0;
            int.TryParse(cond.Value, out value);

            switch (cond.Type)
            {
                case ConditionType.HpLowerThan: return (Host.me.hp < value);
                case ConditionType.HpHigherThan: return (Host.me.hp > value);
                case ConditionType.HppLowerThan: return (Host.me.hpp < value);
                case ConditionType.HppHigherThan: return (Host.me.hpp < value);
                case ConditionType.ManaLowerThan: return (Host.me.mp < value);
                case ConditionType.ManaHigherThan: return (Host.me.mp > value);
                case ConditionType.ManapLowerThan: return (Host.me.mpp < value);
                case ConditionType.ManapHigherThan: return (Host.me.mpp > value);
                case ConditionType.BuffExists: return (IsBuffExists((uint)value));
                case ConditionType.BuffNotExists: return (!IsBuffExists((uint)value));
                case ConditionType.TargetDistLowerThan: return (Host.dist(target) < value);
                case ConditionType.TargetDistHigherThan: return (Host.dist(target) > value);
            }

            return false;
        }


        #region Helpers

        private void SetState(State nstate) => (state) = nstate;

        private void AddTargetLoot(Creature obj) => toLoot.Add(obj.GetHashCode());
        private void CancelMove() => Host.CancelMoveTo();

        private int JunkItemsCount() => (Host.getAllInvItems().Where(i => settings.CleanItems.Contains(i.name)).Count());
        private int Ping() => (Host.pingToServer);

        private bool IsCleanEnabled() => (settings.CleanItems.Count() > 0);
        private bool IsNeedsResting() => (Host.me.hpp <= settings.MinHitpoints || Host.me.mpp <= settings.MinMana);
        private bool AnyBoostExists() => (template.BoostingBuffs.Count > 0);
        private bool ZoneExists() => (fightZone != null);
        private bool SkillExists(uint skillId) => Host.isSkillLearned(skillId);

        // Zone
        private double ZoneRadius
        {
            get { return (fightZone != null) ? fightZone.radius : 0; }
        }

        private double ZoneCenterDist() => (fightZone != null) ? Host.me.dist(fightZone.X, fightZone.Y) : 0;


        private void MoveUnless(Func<bool> eval)
        {
            Task.Run(() =>
            {
                while (isMoving && token.IsAlive())
                {
                    try
                    {
                        if (eval.Invoke())
                        {
                            Utils.Delay(450, 650, token);

                            CancelMove();
                            break;
                        }
                    }
                    catch
                    {
                    }

                    Utils.Delay(50, token);
                }
            }, token);
        }

        #endregion

        #region Events & Handlers

        private void HookGameEvents()
        {
            Host.onLootAvailable += OnLootAvailable;
            Host.onNewSkillLearned += OnNewSkillLearned;
            Host.onNewCreature += OnNewCreature;
            Host.onSkillCasting += OnSkillCasting;
            Host.onNewInvItem += OnNewInvItem;
            Host.onGoldCountChanged += OnGoldAmountChanged;
            Host.onNewBuff += OnNewBuff;
            Host.onExpChanged += OnExpChanged;
            Host.onChatMessage += OnChatMessage;
        }

        private void UnhookGameEvents()
        {
            Host.onLootAvailable -= OnLootAvailable;
            Host.onNewSkillLearned -= OnNewSkillLearned;
            Host.onNewCreature -= OnNewCreature;
            Host.onSkillCasting -= OnSkillCasting;
            Host.onNewInvItem -= OnNewInvItem;
            Host.onGoldCountChanged -= OnGoldAmountChanged;
            Host.onNewBuff -= OnNewBuff;
            Host.onExpChanged -= OnExpChanged;
            Host.onChatMessage -= OnChatMessage;
        }


        private void OnLootAvailable(Creature obj)
        {
            if ((obj.firstHitter != Host.me))
                return;

            stats.MobsKilled += 1;
            UI.AddToGridBag(obj);


            if (settings.LootTargets) AddTargetLoot(obj);
        }

        private void OnNewInvItem(Item item, int count)
        {
            if (item != null)
            {
                UI.AddToGridBag(item, count);
            }
        }

        private void OnSkillCasting(Creature obj, SpawnObject obj2, Skill skill, double x, double y, double z)
        {
            if (obj.type == BotTypes.Player && (obj2 == Host.me) && (skill.id == 20256))
            {
                stats.SuspectReports++;
            }
        }

        private void OnChatMessage(ChatType chatType, string text, string sender)
        {
            if (chatType == ChatType.Whisper)
            {
                stats.WhispersReceived++;
            }
        }

        private void OnExpChanged(Creature obj, int value)
        {
            if ((obj == Host.me))
            {
                stats.ExpGained += value;
            }
        }

        private void OnNewBuff(Buff buff, Creature obj)
        {
        }

        private void OnGoldAmountChanged(int count, ItemPlace place) => stats.GoldEarned += count;
        private void OnNewSkillLearned(Creature obj, Effect skill) => UI.GetSkills();
        private void OnNewCreature(Creature obj) => UI.GetTargets();

        #endregion
    }
}
