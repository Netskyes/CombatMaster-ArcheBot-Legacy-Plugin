using System;
using ArcheBot.Bot.Classes;

namespace CombatMaster
{
    using Data;
    using Enums;

    public static class CoreExtension
    {
        public static double AreaRadius(this Skill skill) => (skill.db.targetAreaRadius);
        public static CastType CastType(this Skill skill) => (CastType)(skill.db.damageTypeId);
        public static SelectType SelectType(this Skill skill) => (SelectType)(skill.db.targetSelectionId);
        public static TargetType TargetType(this Skill skill) => (TargetType)(skill.db.targetTypeId);

        public static string Format(this long value)
        {
            return value.GoldFormat().Format();
        }

        public static Currency GoldFormat(this long goldAmount)
        {
            return new Currency()
            {
                Gold = goldAmount / 10000,
                Silver = (goldAmount % 10000) / 100,
                Copper = (goldAmount % 10000) % 100
            };
        }

        public static string SecToTime(this ulong s)
        {
            return TimeSpan.FromSeconds(s).ToString(@"hh\:mm\:ss");
        }
    }
}
