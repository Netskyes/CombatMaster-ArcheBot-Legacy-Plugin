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
        private State state;
        private Creature target;
        private DateTime? seekTime;

        private List<string> rotation;
        private Dictionary<string, Combos> combos;
        private int sequence;
        private Queue<string> skillLoader;
        
        private RoundZone fightZone;
        private List<string> checkPoints = new List<string>();

        private List<int> toLoot;

        private bool isMoving;
        private bool isFighting;


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
            if (!Host.me.isAlive() && MakeRevival())
            {
                SetState(State.Check);

                return;
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
                // Use recovery of some kind.

                return;
            }


            if (!ZoneExists())
            {
                GenerateFightZone();
            }

            if (!UnderAttack() && !InZoneRadius())
            {
                ComeToFightCenter();
            }


            if (IsCleanEnabled() && JunkItemsCount() >= Utils.RandomNum(1, 4))
            {
                Log("Cleaning inventory...");
                CleanItems();
            }


            SetState(State.Search);
        }

        private void Search()
        {
            if (UnderAttack())
            {
                var mob = Host.getAggroMobs().FirstOrDefault();

                if (mob != null && IsAttackable(mob))
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
            }
            else
            {
                if (seekTime == null)
                    seekTime = DateTime.Now;

                if ((DateTime.Now - seekTime).Value.TotalSeconds >= 30)
                {
                    seekTime = null;
                    SwitchFightZone();

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

            bool isMeTarget = (target.target != null) ? (target.target == Host.me) : true;
            

            if (target != null && isMeTarget && IsAttackable(target))
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
            if (!UnderAttack() && settings.LootTargets && toLoot.Count > 0 && !IsInventoryFull())
            {
                Utils.Delay(new int[] { 250, 450 }, new int[] { 550, 750 }, new int[] { 850, 1050 }, token);

                LootMobs();
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


            var props = SkillHelper.GetProps(skill.id);
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
            Func<bool> moveEval = () => (IsDisabled(Host.me) || (UnderAttack() && !UnderAttackBy(target)));

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
                if (InFight())
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

                if (result || (!result && Host.GetLastError() == LastError.InvalidTarget))
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


        private List<GpsPoint> GetFightPoints() => gps.GetPointsByName("Fight");

        private GpsPoint GetCenterPoint()
        {
            var zone = new RoundZone(fightZone.X, fightZone.Y, 1);

            return GetFightPoints().Where(p => zone.PointInZone(p.x, p.y)).FirstOrDefault();
        }

        private void SwitchFightZone()
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

        private bool InZoneRadius(Creature obj = null) => fightZone.ObjInZone((obj == null) ? Host.me : obj);

        private bool ComeToFightCenter()
        {
            Func<bool> moveEval = () => UnderAttack();

            isMoving = true;
            MoveUnless(moveEval);


            bool result = false;

            if (gps.IsLoaded)
            {
                var point = GetCenterPoint();

                if (point != null)
                {
                    result = gps.MoveToPoint(point);
                }
            }
            else
            {
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
        private bool IsNeedsResting() => (Host.me.hpp < settings.MinHitpoints || Host.me.mpp < settings.MinMana);
        private bool AnyBoostExists() => (template.BoostingBuffs.Count > 0);
        private bool ZoneExists() => (fightZone != null);
        private bool SkillExists(uint skillId) => Host.isSkillLearned(skillId);

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
        }

        private void UnhookGameEvents()
        {
            Host.onLootAvailable -= OnLootAvailable;
            Host.onNewSkillLearned -= OnNewSkillLearned;
            Host.onNewCreature -= OnNewCreature;
            Host.onSkillCasting -= OnSkillCasting;
        }


        private void OnLootAvailable(Creature obj)
        {
            if (!settings.LootTargets || (obj.firstHitter != Host.me))
                return;


            AddTargetLoot(obj);
        }

        private void OnNewSkillLearned(Creature obj, Effect skill) => UI.GetSkills();

        private void OnNewCreature(Creature obj) => UI.GetTargets();

        private void OnSkillCasting(Creature obj, SpawnObject obj2, Skill skill, double x, double y, double z)
        {
            if (obj == Host.me)
            {
            }
        }

        #endregion
    }
}
