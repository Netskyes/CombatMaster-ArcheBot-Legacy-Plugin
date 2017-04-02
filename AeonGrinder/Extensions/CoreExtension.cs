using ArcheBot.Bot.Classes;

namespace AeonGrinder
{
    using Enums;

    public static class CoreExtension
    {
        public static TargetType TargetType(this Skill skill)
        {
            switch (skill.db.targetTypeId)
            {
                case 0:
                    return Enums.TargetType.Self;
                case 4:
                    return Enums.TargetType.Creature;
                case 6:
                    return Enums.TargetType.Location;

                default:
                    return Enums.TargetType.Unknown;
            }
        }
    }
}
