using ArcheBot.Bot.Classes;

namespace AeonGrinder
{
    using Enums;

    public static class CoreExtension
    {
        public static double AreaRadius(this Skill skill) => (skill.db.targetAreaRadius);
        public static CastType CastType(this Skill skill) => (CastType)(skill.db.damageTypeId);
        public static SelectType SelectType(this Skill skill) => (SelectType)(skill.db.targetSelectionId);
        public static TargetType TargetType(this Skill skill) => (TargetType)(skill.db.targetTypeId);
    }
}
