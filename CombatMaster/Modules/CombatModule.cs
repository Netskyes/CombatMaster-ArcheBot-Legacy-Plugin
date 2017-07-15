using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;
using System.Diagnostics;

namespace CombatMaster.Modules
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

        public Creature Target { get; set; }
        public int TargHash { get; set; }
        public Routine Routine { get; set; }
        public bool IsMoving { get; set; }
        public bool IsFighting { get; set; }


        public CombatModule(Host host, CancellationToken token, Settings settings, MemLock memory) : base(host)
        {
            Host = host;
            this.token = token;
            this.settings = settings;
            this.memory = memory;
        }


        public void AttackTarget(bool isManual = false)
        {
            if (Host.isMounted())
            {
                Host.StandFromMount();
            }

            if (Host.me.mpp > 4) { UseNextSkill(isManual); } else { BasicAttack(); }
        }

        public bool IsCastViable()
        {
            var name = GetNextSkill();

            bool isBoost = Routine.IsCombatBuff(name);
            bool isWeapon = IsWeaponUse(name);

            if (isBoost || isWeapon)
            {
                if (!AnyConditionsMeets(name, ConditionType.TargetDistLowerThan, ConditionType.TargetDistHigherThan))
                    return false;
            }

            if (isWeapon)
                return true;


            var skill = Host.getSkill(name);

            if (skill == null)
            {
                if (!Routine.IsCombo(name))
                {
                    Routine.PushNext();
                }
                else
                {
                    Routine.QueueTake();
                }

                return false;
            }


            var castDist = (!isBoost) ? CalcMaxSkillRange(skill) : 0;

            return ((isBoost || castDist < 0) 
                || (Target != null && TargetDist() < 34 && TargetDist() < castDist));
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

        public void NextRoutine()
        {
            var routines = settings.Routines;
            int sequence = routines.IndexOf(Routine.Name);

            int next = (sequence < (routines.Count - 1)) ? (sequence + 1) : 0;
            Routine.Build(routines[next]);
        }

        public bool IsRoutineValid()
        {
            return (Routine != null && Routine.IsValid());
        }

        public string GetNextSkill()
        {
            return (LoaderCount() < 1) ? Routine.GetNext() : Routine.QueuePeek();
        }

        public bool ConditionsMeets(string skillName)
        {
            var conds = Routine.GetConditions(skillName);

            return (conds == null) || conds.All(c => CheckCondition(c));
        }

        public bool AnyConditionsMeets(string skillName, params ConditionType[] conditions)
        {
            var conds = Routine.GetConditions(skillName);

            return (conds == null) || conds.Where
                (c => conditions.Contains(c.Type)).Any(c => CheckCondition(c));
        }

        public bool CanCastSkill(Skill skill, bool isLoaded)
        {
            return (skill != null) && (CanCastSkill(skill) || isLoaded);
        }

        public bool CanCastSkill(string name, bool isLoaded) => CanCastSkill(Host.getSkill(name), isLoaded);

        private bool CanCastNextSkill()
        {
            string name = GetNextSkill();

            return CanCastSkill(name, Routine.IsLoaded(name));
        }

        private Item GetWeapon(string name) => GetUseWeapons().FirstOrDefault(w => w.name == name);

        private bool IsWeaponUse(string name)
        {
            return GetUseWeapons().Any(w => w.name == name);
        }

        private bool CanUseWeapon(string name)
        {
            var weapon = GetWeapon(name);

            return (weapon != null) && Host.itemCooldown(weapon) == 0;
        }

        private void PushNext(bool isLoaded)
        {
            if (!isLoaded)
            {
                Routine.PushNext();
            }
            else
            {
                Routine.QueueTake();
            }
        }


        public bool UseSkill(Skill skill, bool selfTarget = false, bool autoCome = true)
        {
            if (skill == null)
                return false;

            while (IsCasting())
            {
                Utils.Delay(50, token);
            }


            if (autoCome && !selfTarget)
            {
                double dist = CalcMaxSkillRange(skill);

                if (dist > 0)
                {
                    double comeDist = (dist <= 4) ? Utils.Rand(0.8, 2) : dist - 1.5;

                    if ((TargetDist() > dist) && !ComeToTarget(comeDist, false))
                    {
                        // Failed coming to target...
                        return false;
                    }
                }
            }


            bool result = false;

            SelectTarget();

            if (skill.TargetType() != TargetType.Location)
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


            if (!result && autoCome)
            {
                var error = Host.GetLastError();

                if (TargetDist() >= 3.5 && (error == LastError.NoLineOfSight || error == LastError.TargetTooFarAway))
                {
                    ComeToTarget(TargetDist() - 2.5, false);
                }
            }


            return result;
        }

        public bool UseSkill(uint id, bool selfTarget = false, bool autoCome = true)
            => UseSkill(Host.getSkill(id), selfTarget, autoCome);

        public bool UseSkill(string name, bool selfTarget = false, bool autoCome = true)
            => UseSkill(Host.getSkill(name), selfTarget, autoCome);


        public void UseNextSkill(bool isManual = false)
        {
            var name = GetNextSkill();
            var isLoaded = Routine.IsLoaded(name);
 
            // Weapon effects
            if (IsWeaponUse(name))
            {
                if (CanUseWeapon(name) && ConditionsMeets(name) && (GetWeapon(name)?.UseItem() ?? false))
                {
                    Utils.Delay(new int[] { 450, 650 }, new int[] { 550, 850 }, token);
                }

                PushNext(isLoaded);

                return;
            }


            var skill = Host.getSkill(name);

            if (!CanCastSkill(skill, isLoaded))
            {
                Routine.PushNext();

                return;
            }

            if (!ConditionsMeets(name))
            {
                PushNext(isLoaded);

                return;
            }


            var isBoosts = Routine.IsCombatBuff(name);


            bool isCombo = false;
            List<string> combo = null;

            if (!isLoaded && !isBoosts && Routine.IsCombo(name))
            {
                combo = Routine.GetCombo(name);
                isCombo = (combo != null && !combo.Any(s => (Host.getSkill(s) == null) || (Host.skillCooldown(s) > 0)));
            }


            if (UseSkill(skill, isBoosts, (!isBoosts && !isManual)))
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
                if (!memory.IsLocked(name))
                {
                    memory.Lock(name, Utils.Rand(1, 3), Utils.Rand(12, 24), true);

                    return;
                }
                else if (isLoaded)
                {
                    Routine.QueueTake();
                }
            }


            if (!isLoaded)
            {
                Routine.PushNext();
            }
        }


        public void BreakCC()
        {
            var exec = new List<uint>();

            if (Host.isStun())
            {
                uint s1 = Abilities.Auramancy.ShrugItOff;
                uint s2 = Abilities.Songcraft.AlarmCall;

                if (SkillExists(s1))
                    exec.Add(s1);

                if (SkillExists(s2))
                    exec.Add(s2);
            }

            if (Host.isRoot() || Host.isCrippled())
            {
                uint s1 = Abilities.Battlerage.Bondbreaker;

                if (SkillExists(s1))
                    exec.Add(s1);
            }

            if (Host.isSleep())
            {
                uint s1 = Abilities.Songcraft.AlarmCall;
                uint s2 = Abilities.Witchcraft.CourageousAction;

                if (SkillExists(s1))
                    exec.Add(s1);

                if (SkillExists(s2))
                    exec.Add(s2);
            }

            if (Host.isSilense())
            {
                uint s1 = Abilities.Auramancy.Liberation;

                if (SkillExists(s1))
                    exec.Add(s1);
            }


            if (exec.Count < 1)
                return;


            var skills = exec.Select
                (s => Host.getSkill(s)).Where(s => s != null && Host.skillCooldown(s) == 0);

            if (skills.Count() < 1)
                return;


            skills.First()?.UseSkill(false, true);
        }


        public Creature FindTarget(Zone zone = null)
        {
            if (UnderAttack())
            {
                var target = GetAggroTarget();

                if (target != null)
                    return target;
            }


            var targets = Host.getCreatures().Where(c => IsAttackable(c) && !IsUnderAttack(c));

            if (zone != null)
            {
                targets = targets.Where(c => zone.ObjInZone(c));
            }

            if (settings.Targets.Count > 0)
            {
                targets = targets.Where
                    (c => settings.Targets.Contains(c.name)).OrderBy(c => settings.Targets.IndexOf(c.name));
            }

            if (settings.IgnoreTargets.Count > 0)
            {
                targets = targets.Where
                    (c => !settings.Targets.Contains(c.name));
            }

            if (targets.Count() < 1)
                return null;


            return targets.OrderBy(c => Host.dist(c)).FirstOrDefault();
        }

        public Creature GetAggroTarget()
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

        public void GetSelectTarget()
        {
            var target = Host.me.target;

            if (target == null || !IsAttackable(target))
                return;


            SetTarget(target);
        }

        public void SelectTarget(Creature target)
        {
            if (target != null && Host.me.target != target && !Host.isStealth(target))
            {
                Host.SetTarget(target);
            }
        }

        public void SelectTarget() => SelectTarget(Target);


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
                        || (IsAnyAggro() && !UnderAggroBy(Target) && GetAggroTarget() != null) 
                        || (!InFight() && IsUnderAttack(Target));

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


        public void HealUp(int hpp, bool ignoreCombat = false)
        {
            if (!AnyHealsExists())
                return;

            var skills = Routine.GetHeals().Select
                (h => Host.getSkill(h)).Where(h => h != null && Host.skillCooldown(h) == 0);

            if (skills.Count() < 1)
                return;


            foreach (var skill in skills)
            {
                if (Host.me.hpp >= hpp)
                    break;

                if (!ignoreCombat && UnderAttack())
                    break;
                

                if (skill.UseSkill(false, true)) Utils.Delay(450, 850, token);
            }
        }

        public void BoostUp()
        {
            if (!AnyBoostExists())
                return;

            var skills = Routine.GetBoostingBuffs().Select
                (s => Host.getSkill(s)).Where(s => s != null && Host.skillCooldown(s) == 0);

            if (skills.Count() < 1)
                return;


            foreach (var skill in skills)
            {
                if (UnderAttack())
                    break;

                var prods = SkillHelper.GetProdsBuffs(skill.id);

                if (AnyBuffExists(prods))
                    continue;


                if (skill.UseSkill(false, true) && !UnderAttack()) Utils.Delay(450, 850, token);
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
                case ConditionType.AggroCountLowerThan: return (Host.getAggroMobs().Count < value);
                case ConditionType.AggroCountHigherThan: return (Host.getAggroMobs().Count > value);
                case ConditionType.TargetIsNpc: return (Target != null && Target.type == BotTypes.Npc);
                case ConditionType.TargetIsPlayer: return (Target != null && Target.type == BotTypes.Player);
            }

            return false;
        }

        
        #region Helpers

        private int LoaderCount() => Routine.Loader.Count;
        private bool AnyBoostExists() => Routine.GetBoostingBuffs().Count > 0;
        private bool AnyHealsExists() => Routine.GetHeals().Count > 0;

        public bool IsTargetValid() => IsAttackable(Target);
        public bool IsPvPMode() => (IsTargetValid() && Target.type == BotTypes.Player);
        public bool IsManual() => (settings.ManualMovement);
        public Creature GetTarget(Zone zone = null) => (Target = FindTarget(zone));
        
        public void SetTarget(Creature obj) => Target = obj;
        public void ClearTarget() => Target = null;

        public double TargetDist() => (Target != null) ? Host.dist(Target) : 0;


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

        #endregion
    }
}
