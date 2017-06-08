using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;

namespace AeonGrinder.Modules
{
    using Data;
    using Enums;
    using Configs;

    internal class CombatModule : CoreHelper
    {
        private Host Host;
        private Settings settings;
        private CancellationToken token;
        private MemLock memory;


        public ControlType ControlType = ControlType.Auto;
        public Routine Routine { get; set; }
        public Creature Target { get; set; }

        public bool IsMoving { get; set; }
        public bool IsFighting { get; set; }

        
        public CombatModule(Host host, CancellationToken token, Settings settings, MemLock memory) : base(host)
        {
            Host = host;
            this.token = token;
            this.settings = settings;
            this.memory = memory;

            // Set combat type considering settings
        }

        public bool IsRoutineValid()
        {
            return (Routine != null && Routine.IsValid());
        }


        private Creature FindTarget(Zone zone = null)
        {
            if (UnderAttack())
            {
                return GetAggroTarget();
            }


            var targets = Host.getCreatures().Where(c => IsAttackable(c) && !IsUnderAttack(c));

            if (zone != null)
            {
                targets = targets.Where(c => zone.ObjInZone(c));
            }

            if (settings.Targets.Count > 0)
            {
                targets = targets.Where(c => settings.Targets.Contains(c.name));
            }

            if (targets.Count() < 1)
                return null;


            return targets.OrderBy(c => Host.dist(c)).FirstOrDefault();
        }

        private Creature GetAggroTarget()
        {
            var mobs = Host.getAggroMobs();
            var fams = Host.getMounts();

            if (mobs.Count() < 1 && fams.Count > 0)
            {
                foreach (var f in fams)
                {
                    mobs = mobs.Concat(GetTargetAggro(f)).ToList();
                }
            }

            var targets = mobs.Where
                (m => IsAttackable(m) && Host.dist(m) < 60);

            if (targets.Count() < 1)
                return null;


            return targets.OrderBy(m => Host.dist(m)).FirstOrDefault();
        }


        public bool ComeToTarget(double dist, bool overwatch = true)
        {
            if (Target == null)
                return false;

            if (Host.dist(Target) <= dist)
                return true;


            IsMoving = true;

            if (overwatch)
            {
                Task.Run(() => MoveWatch(), token);
            }

            bool result = Host.ComeTo(Target, dist, dist + Utils.Rand(0.6, 1.1));
            IsMoving = false;

            return result;
        }

        private void MoveWatch()
        {
            while (token.IsAlive() && IsMoving)
            {
                try
                {
                    bool eval = IsDisabled(Host.me) 
                        || (IsAnyAggro() && !UnderAggroBy(Target)) || (!InFight() && IsUnderAttack(Target));

                    if (eval)
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
        }


        public void AttackTarget()
        {
            if (Host.me.mpp > 4)
            {
                UseNextSkill();
            }
            else
            {
                BasicAttack();
            }
        }

        public void AntiCC()
        {
            // Anti CC skills
        }


        public void UseNextSkill()
        {
            var name = "";
            var isLoaded = false;

            if (LoaderCount() < 1)
            {
                name = Routine.GetNext();
            }
            else
            {
                name = Routine.QueuePeek();
                isLoaded = true;
            }


            var skill = Host.getSkill(name);

            if (skill == null || (!isLoaded && !CanCastSkill(skill)))
            {
                Routine.PushNext();
                
                return;
            }


            var isBoosts = Routine.IsCombatBuff(name);


            bool isCombo = false;
            List<string> combo = null;

            if (!isLoaded && !isBoosts && Routine.IsCombo(name))
            {
                var isValid = true; // Routine.Combos[name].Conditions.All(c => CheckCondition(c));

                if (isValid)
                {
                    combo = Routine.Combos[name].Skills;

                    isCombo = (combo != null && combo.Count > 0 && !combo.Any(s => (Host.getSkill(s) == null) || (Host.skillCooldown(s) > 0)));
                }
            }


            if (UseSkill(skill, isBoosts, !isBoosts))
            {
                if (isLoaded)
                {
                    Routine.QueueTake();
                }
                else if (isCombo)
                {
                    Routine.LoadCombo(name);
                }


                Utils.Delay(new int[] { 50, 100 }, new int[] { 100, 150 }, new int[] { 150, 200 }, token);
            }
            else
            {
                if (!memory.IsLocked("Retry") && Host.GetLastError() == LastError.ActionNotAllowed)
                {
                    memory.Lock("Retry", Utils.Rand(1, 3), 
                        Utils.Rand(3, 6), true);

                    return;
                }
            }


            if (!isLoaded)
            {
                Routine.PushNext();
            }
        }

        public bool UseSkill(Skill skill, bool selfTarget = false, bool autoCome = true)
        {
            while (IsCasting())
            {
                Utils.Delay(50, token);
            }

            bool isLocTarget = (skill.TargetType() == TargetType.Location);


            if (autoCome && !selfTarget)
            {
                double dist = CalcMaxSkillRange(skill);

                if (dist > 0)
                {
                    double comeDist = (dist <= 4) ? Utils.Rand(0.8, 2) : dist - 1.5;

                    if ((TargetDist() > dist) && !ComeToTarget(comeDist))
                    {
                        // Failed coming to target...
                        return false;
                    }
                }
            }



            if (Host.me.target != Target)
                Host.SetTarget(Target);
            

            bool result = false;

            if (!isLocTarget)
            {
                result = skill.UseSkill(false, selfTarget);
            }
            else
            {
                result = Host.UseSkill(skill.id, Target.X, Target.Y, Target.Z, false);
            }


            while (IsCasting())
            {
                Utils.Delay(50, token);
            }

            if (Host.GetLastError() == LastError.NoLineOfSight && TargetDist() > 4)
            {
                ComeToTarget(TargetDist() - 2.5);
            }


            return result;
        }

        public void BasicAttack()
        {
            if (Host.dist(Target) <= 2)
            {
                Host.UseSkill(3);
            }
            else
            {
                Host.UseSkill(16064);
            }
        }


        public void BoostUp()
        {
            if (!AnyBoostExists())
                return;


            foreach (var name in Routine.Template.BoostingBuffs)
            {
                if (UnderAttack())
                    break;

                var skill = Host.getSkill(name);

                if (skill == null)
                    continue;


                var prods = SkillHelper.GetProdsBuffs(skill.id);

                if (AnyBuffExists(prods))
                    continue;


                if (Host.UseSkill(name, false, true) && !UnderAttack())
                {
                    Utils.Delay(450, 850, token);
                }
            }
        }


        private bool CheckCondition(Condition cond)
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
                case ConditionType.BuffExists: return (BuffExists((uint)value));
                case ConditionType.BuffNotExists: return (!BuffExists((uint)value));
                case ConditionType.TargetDistLowerThan: return (Host.dist(Target) < value);
                case ConditionType.TargetDistHigherThan: return (Host.dist(Target) > value);
            }

            return false;
        }


        #region Helpers

        private int LoaderCount() => Routine.Loader.Count;
        private bool AnyBoostExists() => Routine.Template.BoostingBuffs.Count > 0;

        public bool IsTargetValid() => (Target != null && IsAttackable(Target));
        public bool IsManual() => (settings.ManualMovement);
        public Creature GetTarget(Zone zone = null) => (Target = FindTarget(zone));
        
        public void SetTarget(Creature obj) => Target = obj;
        public void ClearTarget() => Target = null;

        private double CalcMaxSkillRange(Skill skill)
        {
            double dist = Host.me.calcSkillMaxRange(skill.id);

            if (dist == 0)
            {
                if (skill.SelectType() == SelectType.None && skill.CastType() == CastType.Magic)
                {
                    // Damage area radius
                    dist = (skill.AreaRadius() - Utils.Rand(0.5, 1.2));
                }
            }

            return dist;
        }

        public double TargetDist() => (Target != null) ? Host.dist(Target) : 0;

        #endregion
    }
}
