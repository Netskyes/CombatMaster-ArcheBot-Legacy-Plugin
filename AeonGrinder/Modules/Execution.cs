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
        private Statistics stats;
        
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


            combat.Routine = new Routine(Host, UI.GetTemplates());
            combat.Routine.Build(settings.TemplateName);
            
            if (!combat.IsRoutineValid())
            {
                Log("Please build your routine before starting!");

                return false;
            }

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

            return true;
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
                MakeRevival();
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
            if (IsManual())
            {
                SetState(State.Fight);

                return;
            }

            if (!UnderAttack() && !IsPeaceZone())
            {
                if (NeedsResting())
                {
                    RecoverStats();
                }
                
                if (!token.IsAlive())
                    return;
            }


            if (!UnderAttack())
            {
                CheckTasks();
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


            if (combat.GetTarget(nav.FightZone) != null)
            {
                memory.UnlockTime("Seek");

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

        private void Move()
        {
            if (!ComeToTarget(Utils.Rand(14, 18)))
            {
                SetState(State.Search);
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
            if (!isFighting && !IsManual())
            {
                isFighting = true;
                Task.Run(() => FightWatch(), token);
            }

            if (IsCasting())
                return;

            if (IsManual())
            {
                combat.SetTarget(Host.me.target);
            }

            if (combat.IsTargetValid())
            {
                if (Host.me.target != combat.Target)
                {
                    if (Utils.Rand(0, 2) == 0) Host.SetTarget(combat.Target);
                }

                if (!IsDisabled(Host.me) && !IsSilenced(Host.me))
                {
                    combat.AttackTarget();
                }
                else
                {
                    combat.AntiCC();
                }

                return;
            }

            Host.CancelSkill();

            combat.Routine.EmptyLoader();
            isFighting = false;
            

            SetState(State.Analyze);
        }

        private void Analyze()
        {
            if (IsAnyAggro())
            {
                SetState((IsManual()) ? State.Fight : State.Check);

                return;
            }

            if (settings.LootTargets && lootBag.Count > 0)
            {
                if (!memory.IsLocked("LootMobs") && !IsInventoryFull())
                {
                    Utils.Delay(new int[] { 250, 450 }, new int[] { 550, 750 }, new int[] { 850, 1050 }, token);

                    LootMobs();


                    memory.Lock("LootMobs", Utils.Rand(1, 2), Utils.Rand(5, 12), true);
                }
            }



            SetState(State.Check);
        }


        private void CheckTasks()
        {
            if (IsProcessEnabled() && !memory.IsLocked("Process"))
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

            Task.Run(() => MoveWatch(), token);

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
                    if (combat.TargetDist() <= 8)
                    {
                        Host.TurnDirectly(combat.Target);
                    }

                    if (combat.TargetDist() < 0.4)
                    {
                        Host.MoveBackward(true);
                    }
                    else
                    {
                        Host.MoveBackward(false);
                    }
                }
                catch
                {
                }

                Utils.Delay(50, token);
            }

            isFighting = false;
            Host.MoveBackward(false);
        }

        
        private bool FixCenterDist()
        {
            return (nav.FightRadius >= 80 && nav.FightCenterDist() > (nav.FightRadius / 2));
        }

        private bool ComeToCenter(double dist = 0)
        {
            isMoving = true;
            bool result = false;

            if (nav.IsGpsZone)
            {
                var point = nav.GetFightPoint();

                if (point != null)
                {
                    MoveUnless(() => UnderAttack() || (dist != 0 && Host.me.dist(point.x, point.y) <= dist));
                    result = gps.MoveToPoint(point);
                }
            }
            else
            {
                MoveUnless(() => UnderAttack()
                    || (dist != 0 && Host.me.dist(nav.FightZone.X, nav.FightZone.Y) <= dist));

                result = Host.MoveTo(nav.FightZone.X, nav.FightZone.Y, 0);
            }

            isMoving = false;

            return result;
        }


        #region Helpers

        private void SetState(State nstate) => (state) = nstate;
        private void FlagFightZone() => nav.FlagFightZone();
        private void AddTargetLoot(Creature obj) => lootBag.Add(obj.GetHashCode());

        private void Jump()
        {
            Host.Jump(true);
            Utils.Delay(100, token);
            Host.Jump(false);
        }

        private int JunkItemsCount() => (Host.getAllInvItems().Where(i => settings.CleanItems.Contains(i.name)).Count());
        private int Ping() => (Host.pingToServer);

        private bool IsCleanEnabled() => (settings.CleanItems.Count() > 0);
        private bool IsProcessEnabled() => (settings.ProcessItems.Count() > 0);
        private bool NeedsResting() => (Host.me.hpp <= settings.MinHitpoints || Host.me.mpp <= settings.MinMana);
        private bool SkillExists(uint skillId) => Host.isSkillLearned(skillId);
        private bool IsManual() => (settings.ManualMovement);


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
