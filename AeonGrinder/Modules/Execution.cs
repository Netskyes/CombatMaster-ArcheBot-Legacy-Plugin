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

        private List<string> rotation;
        private Dictionary<string, Combos> combos;
        private int sequence;
        private Queue<string> skillLoader = new Queue<string>();
        

        private Point3D centerPoint;

        private List<int> targetHashes = new List<int>();
        private List<int> toLoot = new List<int>();

        private bool isMoving;
        private bool isFighting;


        /// <summary>
        /// Initialize runtime.
        /// </summary>
        private bool Initialize()
        {
            BuildZone();

            if (template.Rotation.Count < 1)
            {
                Log("Please build your routine before starting!");

                return false;
            }
            else
            {
                BuildRoutine();
            }
            

            // Resets
            SetState(State.Check);

            // Events
            HookGameEvents();


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


        private void BuildZone()
        {
            if (settings.MapName == string.Empty)
            {
                // Set initial center
                centerPoint = new Point3D(new double[] { Host.me.X, Host.me.Y, Host.me.Z });

                return;
            }

            if (!gps.Load(settings.MapName))
                return;


            var point = gps.GetPoint("Zone");

            if (point != null)
            {
                centerPoint = new Point3D(new double[] { point.x, point.y, point.z });
            }
        }

        private void BuildRoutine()
        {
            rotation = template.CombatBuffs;
            rotation = rotation.Concat(template.Rotation).ToList();

            combos = template.Combos.ToDictionary(c => c.Name, c => c);
        }



        private void Execute()
        {
            if (!CriticalCheck())
                return;


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


        private bool CriticalCheck()
        {
            if (!Host.me.isAlive())
            {
                SetState(State.Check); return MakeRevival();
            }

            return true;
        }

        private void Check()
        {
            if (!UnderAttack() && (Host.me.mpp < 30 || Host.me.hpp < 30))
                return;


            if (!InZoneRadius() && !MoveToZone())
            {
            
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
                SetState((Host.dist(target) > 20 ? State.Move : State.Fight));
            }
        }

        private void Move()
        {
            if (ComeToTarget(Utils.RandomNum(14, 18)))
            {
                SetState(State.Fight);
            }
            else
            {
                SetState(State.Search);
            }
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

            bool isMeTarget = (UnderAttack()) ? (IsUnderAttack(target) && UnderAttackBy(target)) : true;
            

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
            isFighting = false;

            Utils.Delay(450, 650, token);


            SetState(State.Analyze);
        }

        private void Analyze()
        {
            if (!UnderAttack() && settings.LootTargets && toLoot.Count > 0 && !IsInventoryFull())
            {
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
                name = skillLoader.Dequeue();
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


            bool result = false;

            if (skill.TargetType() != TargetType.Location)
            {
                result = UseSkill(skill, false, isBoosts, !isBoosts);
            }
            else
            {
                result = UseSkill(skill, true);
            }


            if (result)
            {
                Log($"Used: {name}");
                
                if (props.OptimalWait != 0)
                {
                    Utils.Delay(props.OptimalWait, token);
                }
                else
                {
                    while (IsCasting())
                    {
                        Utils.Delay(50, token);
                    }
                }

                if (!isLoaded && isCombo)
                    LoadCombo(name);
            }
            else
            {
                //Log("Couldn't use: " + skill.name + " / Reason: " + Host.GetLastError());
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



        private bool UseSkill(Skill skill, bool isLocTarget = false, bool selfTarget = false, bool autoCome = true)
        {
            while (IsCasting())
            {
                Utils.Delay(50, token);
            }

            
            double dist = Host.me.calcSkillMaxRange(skill.id);

            if (autoCome && dist != 0)
            {
                double comeDist = (dist <= 4) ? Utils.RandomDouble(0.8, 2) : dist - 1.5;
                
                if ((Host.dist(target) > dist) && !ComeToTarget(comeDist))
                    return false;
            }


            if (Host.me.target != target)
                Host.SetTarget(target);
            
            if (!isLocTarget)
            {
                return skill.UseSkill(false, selfTarget);
            }
            else
            {
                return Host.UseSkill(skill.id, target.X, target.Y, target.Z, false);
            }
        }

        private void FightWatch()
        {
            while (isFighting && token.IsAlive())
            {
                try
                {
                    if (Host.dist(target) <= 5)
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
        {
            target = obj;

            if (target != null) AddTargetHash(obj);
        }

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


        private bool MoveToZone()
        {
            Func<bool> moveEval = () => UnderAttack();

            isMoving = true;
            MoveUnless(moveEval);


            bool result = false;

            if (gps.IsLoaded && gps.PointExists("Zone"))
            {
                result = gps.MoveToPoint("Zone");
            }
            else
            {
                result = Host.MoveTo(centerPoint.X, centerPoint.Y, centerPoint.Z);
            }

            isMoving = false;

            return result;
        }

        private bool InZoneRadius(Creature obj = null)
        {
            var zone = new RoundZone(centerPoint.X, centerPoint.Y, settings.FightRadius);

            return zone.ObjInZone((obj == null) ? Host.me : obj);
        }

        public bool CheckCondition(Condition cond)
        {
            int value = 0;
            int.TryParse(cond.Value, out value);

            switch (cond.Type)
            {
                case ConditionType.HpLowerThan: return (Host.me.hp < value);
                case ConditionType.HppLowerThan: return (Host.me.hpp < value);
                case ConditionType.ManaLowerThan: return (Host.me.mp < value);
                case ConditionType.ManapLowerThan: return (Host.me.mpp < value);
                case ConditionType.BuffExists: return (IsBuffExists((uint)value));
            }

            return false;
        }


        #region Helpers

        private void SetState(State nstate) => (state) = nstate;

        private void AddTargetHash(Creature obj) => targetHashes.Add(obj.GetHashCode());
        private void AddTargetLoot(Creature obj) => toLoot.Add(obj.GetHashCode());
        private void CancelMove() => Host.CancelMoveTo();

        private int JunkItemsCount() => (Host.getAllInvItems().Where(i => settings.CleanItems.Contains(i.name)).Count());
        private int PingTime() => (Host.pingToServer + Utils.RandomNum(50, 100));

        private bool IsCleanEnabled() => (settings.CleanItems.Count() > 0);

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
        }

        private void UnhookGameEvents()
        {
            Host.onLootAvailable -= OnLootAvailable;
            Host.onNewSkillLearned -= OnNewSkillLearned;
            Host.onNewCreature -= OnNewCreature;
        }


        private void OnLootAvailable(Creature obj)
        {
            if (!settings.LootTargets || !targetHashes.Contains(obj.GetHashCode()))
                return;


            targetHashes.Remove(obj.GetHashCode());

            AddTargetLoot(obj);
        }

        private void OnNewSkillLearned(Creature obj, Effect skill) => UI.GetSkills();

        private void OnNewCreature(Creature obj) => UI.GetTargets();

        #endregion
    }
}
