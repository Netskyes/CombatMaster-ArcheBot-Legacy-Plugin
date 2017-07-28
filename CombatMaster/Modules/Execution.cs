using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;
using System.Diagnostics;

namespace CombatMaster.Modules
{
    using Data;
    using Enums;
    
    public sealed partial class BaseModule : CoreHelper
    {
        private State state;
        private Statistics stats;
        private Point3D beginPos;
        private bool isMoving;
        private bool isFighting;
        private List<int> lootBag;
        
        /// <summary>
        /// Initialize runtime.
        /// </summary>
        private bool Initialize()
        {
            if (!nav.BuildZone())
                return false;

            if (settings.Routines.Count < 1)
            {
                Log("Please add at least one routine before starting!");

                return false;
            }

            combat.Routine = new Routine(Host, UI.GetTemplates());
            combat.Routine.Build(settings.Routines[0]);

            if (settings.SwitchRoutines)
                LockSwitches();
            

            // Statistics
            if (stats == null || UI.StatsReset())
            {
                stats = new Statistics(UI);
            }
            
            
            // Events
            HookGameEvents();

            // Resets
            SetState(State.Check);
            lootBag = new List<int>();
            beginPos = null;


            return true;
        }

        

        private void Execute()
        {
            CriticalCheck();

#if DEBUG
            Log("State: " + state);
#endif

            switch (state)
            {
                case State.Check:
                    Check();
                    break;

                case State.Search:
                    Search();
                    break;

                case State.TargetSearch:
                    TargetSearch();
                    break;

                case State.TargetTags:
                    TargetTags();
                    break;

                case State.Move:
                    Move();
                    break;

                case State.Prepare:
                    Prepare();
                    break;

                case State.Fight:
                    Fight();
                    break;

                case State.Analyze:
                    Analyze();
                    break;
            }
        }


        private void CriticalCheck()
        {
            if (combat.IsPvPMode())
                return;


            if (!Host.me.isAlive())
            {
                stats.Deaths += 1;

                if (!IsManual())
                {
                    MakeRevival();
                    SetState(State.Check);
                }
                
                return;
            }

            if (IsBreathCritical() && !TryDahutaBubble())
            {
                GoToSurface();

                return;
            }

            if (state == State.Fight && Host.me.hpp < 12)
            {
                if (UseHealsPots())
                {
                    Utils.Delay(850, 1250, token);

                    if (Host.me.hpp > 15)
                        return;
                }

                if (settings.EscapeDeath && !memory.IsLocked("Death"))
                {
                    if (EscapeDeath())
                    {
                        memory.Lock("Death", 24 * 1000);

                        SleepWhile(() => (UnderAttack() || IsDangerInZone(8)), 50);


                        SetState(State.Check);
                    }
                }
            }
        }

        private void Check()
        {
            if (IsManual())
            {
                SetState(State.Fight);

                return;
            }

            if (!IsAnyAggro() && NeedsResting() && !IsPeaceZone())
            {
                RecoverStats();
                
                if (!token.IsAlive())
                    return;
            }

            if (!UnderAttack())
            {
                CheckTasks();

                if (!token.IsAlive())
                    return;

                SleepWhile(() => BuffExists(1128), 400);
            }

            if (settings.LevelFamiliars)
            {
                ManageFams();
            }


            SetState(State.Search);
        }

        private void Search()
        {
            nav.CheckFightZone();

            if (!UnderAttack() && !nav.InFightRadius())
            {
                ComeToCenter();

                if (!token.IsAlive())
                    return;
            }

            if (IsLeaderAssist())
            {
                SetState(State.TargetSearch);

                return;
            }

            if (IsMobTagging() && !IsAnyAggro() && GetTagTargetsCount() > 1)
            {
                SetState(State.TargetTags);

                return;
            }


            if (combat.GetTarget(nav.FightZone) != null)
            {
                memory.UnlockTime("Seek");

                if (Host.me.target != combat.Target)
                {
                    if (Utils.Rand(0, 2) == 0) combat.SelectTarget();
                }


                SetState((combat.TargetDist() > 20 ? State.Move : State.Prepare));

                return;
            }


            if (FixCenterDist())
            {
                ComeToCenter(Utils.Rand(4, (double)39));

                return;
            }

            if (nav.IsGpsZone)
            {
                memory.TimeLock("Seek", Utils.Rand(18, 30), () => 
                {
                    FlagFightZone();
                    SetState(State.Check);
                });
            }
        }

        private void TargetSearch()
        {
            if (IsLeaderAssist())
            {
                var target = combat.GetAggroTarget();

                if (target != null)
                {
                    combat.SetTarget(target);
                    SetState(State.Fight);

                    return;
                }


                FollowLeader();
                GetLeadersTarget();

                if (!combat.IsTargetValid())
                    return;


                SetState((combat.TargetDist() > 20) ? State.Move : State.Fight);
            }
        }

        private void TargetTags()
        {
            int mobsCount = (int)Host.getAggroMobsCount();
            var targets = combat.GetAllTargets(nav.FightZone).OrderBy(c => Host.dist(c));

            if (beginPos != null)
            {
                targets = targets.Where(c => c.dist(beginPos.X, beginPos.Y) <= 50).OrderBy(c => Host.dist(c));
            }


            var target = (targets.Count() > 0) ? targets.FirstOrDefault() : null;

            if (target == null || mobsCount >= settings.TagMobs)
            {
                if (Host.getAggroMobs().All(m => Host.dist(m) < 4))
                {
                    beginPos = null;
                    SetState(State.Search);
                }

                return;
            }

            
            if (!moving.ComeTo(target, Utils.Rand(14, 18), 15, () => Host.getAggroMobsCount() > mobsCount))
                return;

            if (beginPos == null)
            {
                beginPos = new Point3D(new double[] { Host.me.X, Host.me.Y, Host.me.Z });
            }


            if (Host.me.target != target && !Host.isStealth(target))
            {
                Host.SetTarget(target);
            }

            if (Host.UseSkill(16064))
            {
                Utils.Delay(350, 450, token);
            }
        }

        private void Move()
        {
            if (!ComeToTarget(Utils.Rand(10, 18)))
            {
                SetState((IsLeaderAssist()) ? State.TargetSearch : State.Search);
            }
            else
            {
                Utils.Delay(new int[] { 225, 450 }, new int[] { 525, 750 }, new int[] { 825, 1050 }, token);
                SetState(State.Prepare);
            }
        }

        private void Prepare()
        {
            CombatBoosts();
            combat.BoostUp();
            
            SetState(State.Fight);
        }

        private void Fight()
        {
            if (!isFighting)
            {
                isFighting = true;
                
                if (!IsManual())
                {
                    Task.Run(() => FightWatch(), token);
                }
            }


            if (IsCasting())
                return;

            if (!combat.IsPvPMode())
                CheckHealth();


            if (IsManual())
            {
                combat.GetSelectTarget();

                if (combat.Target == null)
                    return;

                if (combat.IsTargetValid() && !combat.IsCastViable())
                    return;
            }
            

            if (combat.IsTargetValid())
            {
                if (Host.me.target != combat.Target)
                {
                    if (Utils.Rand(0, 2) == 0) combat.SelectTarget();
                }

                if (!IsDisabled(Host.me) && !IsSilenced(Host.me))
                {
                    if (IsSlowDown(Host.me))
                    {
                        BreakCC();
                    }
                    
                    combat.AttackTarget(IsManual());
                }
                else
                {
                    BreakCC();
                }


                memory.Lock("Attacks", Utils.Rand(4, 6), Utils.Rand(24, 36), true);

                if (!combat.IsPvPMode() && memory.IsLocked("Attacks"))
                {
                    Utils.Delay(new int[] { 225, 325 }, new int[] { 250, 375 }, new int[] { 350, 425 }, token);
                }

                return;
            }

            Host.CancelSkill();


            combat.ClearTarget();
            combat.Routine.EmptyLoader();

            isFighting = false;
            

            if (settings.SwitchRoutines && !memory.IsLocked("Switch"))
            {
                combat.NextRoutine();
                LockSwitches();
            }


            SetState(State.Analyze);
        }

        private void Analyze()
        {
            if (IsAnyAggro())
            {
                SetState(State.Check);

                return;
            }

            if (settings.LootTargets && lootBag.Count > 0)
            {
                if (!memory.IsLocked("LootMobs") && !IsInventoryFull())
                {
                    Utils.Delay(new int[] { 450, 650 }, new int[] { 750, 950 }, new int[] { 1150, 1350 }, token);

                    LootMobs();


                    memory.Lock("LootMobs", Utils.Rand(1, 2), Utils.Rand(36, 48), true);
                }
            }


            // Anti pattern.
            if (!UnderAttack() && !IsManual())
            {
                Utils.Delay(new int[] { 850, 1250 }, new int[] { 1250, 1650 }, new int[] { 1650, 2050 }, token);
            }

            SetState(State.Check);
        }


        private void CheckTasks()
        {
            if (IsProcessEnabled() && !memory.IsLocked("Process") && ProcessItemsCount() >= Utils.Rand(14, 30))
            {
                ProcessItems();

                memory.Lock("Process", Utils.Rand(2, 6), Utils.Rand(1, 3), true);
            }


            if (!token.IsAlive() || UnderAttack())
                return;

            if (IsCleanEnabled() && !memory.IsLocked("Clean") && JunkItemsCount() >= Utils.Rand(1, 4))
            {
                CleanItems();

                memory.Lock("Clean", Utils.Rand(1, 3), Utils.Rand(4, 8), true);
            }
        }


        private bool ComeToTarget(double dist, bool overwatch = true)
        {
            isMoving = true;

            if (!Host.me.isSwim)
            {
                Task.Run(() => MoveWatch(), token);
            }
            
            bool result = combat.ComeToTarget(dist, overwatch);
            isMoving = false;

            return result;
        }

        private void MoveWatch()
        {
            while (token.IsAlive() && isMoving)
            {
                try
                {
                    if (!memory.IsLocked("Jump") && Utils.Rand(0, 100) == 1)
                    {
                        Jump();

                        memory.Lock("Jump", Utils.Rand(1, 3), Utils.Rand(1, 3));
                    }
                }
                catch
                {
                }

                Utils.Delay(500, token);
            }
        }

        private void FightWatch()
        {
            while (token.IsAlive() && (isFighting && state == State.Fight))
            {
                try
                {
                    if (!Host.me.isSwim)
                    {
                        if (combat.TargetDist() <= 8 && !IsFacingAngle(combat.Target.X, combat.Target.Y, 40))
                        {
                            Host.TurnDirectly(combat.Target);
                        }
                    }

                    if (!memory.IsLocked("MoveBack") && combat.TargetDist() < 0.4)
                    {
                        Host.MoveBackward(true);
                    }
                    else
                    {
                        Host.MoveBackward(false);
                        memory.Lock("MoveBack", 2, 2);
                    }
                }
                catch
                {
                }

                Utils.Delay(50, token);
            }

            isFighting = false;

            Host.MoveBackward(false);
            Host.MoveLeft(false);
            Host.MoveRight(false);
        }

        
        private bool FixCenterDist()
        {
            return (nav.FightRadius >= 80 && nav.FightCenterDist() > (nav.FightRadius / 2));
        }

        private bool ComeToCenter(double dist = 0)
        {
            if (nav.IsGpsZone)
            {
                var point = nav.GetFightPoint();

                return point != null && moving.ComeToPoint(point, 1, 1.5, () =>
                    UnderAttack() || (dist != 0 && Host.me.dist(point.x, point.y) <= dist));
            }
            else
            {
                return moving.ComeTo(nav.FightZone.X, nav.FightZone.Y, 0, 1, 1.5, () =>
                    UnderAttack() || (dist != 0 && Host.me.dist(nav.FightZone.X, nav.FightZone.Y) <= dist));
            }
        }


        private void FollowLeader()
        {
            if (!InParty())
                return;

            var leader = Host.getPartyLeaderObj();

            if (leader == null)
                return;


            if (Host.dist(leader) < 8)
                return;


            isMoving = true;

            bool result = moving.ComeTo(leader, 1, 1.5, () => IsDisabled(Host.me) || IsAnyAggro());
            isMoving = false;
        }

        private void GetLeadersTarget()
        {
            if (!InParty())
                return;

            var leader = Host.getPartyLeaderObj();

            if (leader == null)
                return;


            if (leader.target != null)
            {
                combat.SetTarget(leader.target);
            }
        }

        private void CheckHealth()
        {
            if (memory.IsLocked("Healing"))
                return;

            int minHpp = combat.Routine.Template.MinHpHeals;

            if (Host.me.hpp >= minHpp)
                return;


            combat.HealUp(minHpp, true);
            memory.Lock("Healing", 2, 2, true);
        }

        private bool IsBreathCritical()
        {
            if (!Host.me.isUnderWaterBreath)
                return false;

            return Host.getInvItem(29300) == null && (Host.buffTime(6660) < (40 * 1000) || Host.me.underWaterBreathTime < (20 * 1000));
        }


        #region Helpers

        private void SetState(State nstate) => (state) = nstate;
        private void FlagFightZone() => nav.FlagFightZone();
        private void AddTargetLoot(Creature obj) => lootBag.Add(obj.GetHashCode());
        private void LockSwitches() => memory.Lock("Switch", Utils.Rand(2, 7), 0, true);

        private void BreakCC()
        {
            if (settings.AntiCC) combat.BreakCC();
        }

        private void Jump()
        {
            Host.Jump(true);
            Utils.Delay(100, token);
            Host.Jump(false);
        }

        private int JunkItemsCount() => (Host.getAllInvItems().Where(i => settings.CleanItems.Contains(i.name)).Count());
        private int ProcessItemsCount() => (Host.getAllInvItems().Where(i => settings.ProcessItems.Contains(i.name)).Count());
        private int Ping() => (Host.pingToServer);
        private int GetTagTargetsCount() => GetTagTargets().Count();

        private bool IsCleanEnabled() => (settings.CleanItems.Count() > 0);
        private bool IsProcessEnabled() => (settings.ProcessItems.Count() > 0);
        private bool NeedsResting() => (Host.me.hpp <= settings.MinHitpoints || Host.me.mpp <= settings.MinMana);
        private bool IsManual() => (settings.ManualMovement);
        private bool IsLeaderAssist() => (InParty() && settings.AssistLeader);
        private bool IsMobTagging() => (settings.MobTagging);

        public void SleepWhile(Func<bool> eval, int sleep)
        {
            if (eval == null)
                return;

            while (eval.Invoke())
            {
                Utils.Delay(sleep, token);
            }
        }

        private IEnumerable<Creature> GetTagTargets()
        {
            var targets = combat.GetAllTargets(nav.FightZone);

            if (beginPos != null)
            {
                return targets.Where(c => c.dist(beginPos.X, beginPos.Y) <= 50);
            }
            else
            {
                return targets.Where(c => Host.dist(c) <= 50);
            }
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
            Host.onLootDice += OnLootDice;
            Host.onRaidInvite += OnRaidInvite;
            Host.onMapPosChanged += OnMapPosChanged;
            Host.onUnitImmune += OnUnitImmune;
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
            Host.onLootDice -= OnLootDice;
            Host.onRaidInvite -= OnRaidInvite;
            Host.onMapPosChanged -= OnMapPosChanged;
            Host.onUnitImmune -= OnUnitImmune;
        }


        private void OnUnitImmune(Creature obj)
        {
            if (IsManual())
                return;

            if (obj == null || combat.Target != obj)
                return;


            var unique = obj.GetHashCode().ToString();
            var immune = Host.GetVar(obj, "Immunity");

            if (!memory.IsLocked(unique))
            {
                Host.SetVar(obj, "Immunity", false);
                var isLocked = memory.Lock(unique, 5 * 1000, 8);
                
                if (isLocked)
                {
                    Task.Run(() =>
                    {
                        Utils.Delay(4 * 1000, token);
                        Host.SetVar(obj, "Immunity", false);

                    }, token);
                }
            }
            else
            {
                Host.SetVar(obj, "Immunity", true);
            }
        }

        private void OnMapPosChanged(double X, double Y, double Z)
        {
            
        }

        private void OnRaidInvite(string nick)
        {
            if (!IsLeaderAssist())
            {
                Host.LeaveFromParty();
            }
        }

        private void OnLootDice(Item item)
        {
            if (item != null && settings.RollOnItems)
            {
                item.Dice(true);      
            }
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
