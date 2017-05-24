using System.Collections.Generic;
using System.Linq;

namespace AeonGrinder
{
    using Data;

    public static class SkillHelper
    {
        private static readonly Dictionary<uint, SkillProps> skills = new Dictionary<uint, SkillProps>()
        {
            { Abilities.Sorcery.MagicCircle, new SkillProps() { LockMove = true } }
        };

        private static Dictionary<uint, uint[]> producedBuffs = new Dictionary<uint, uint[]>()
        {
            { Abilities.Auramancy.HealthLift, new uint[] { 794, 795, 796, 841, 1423, 1424, 1425, 7655, 13867, 13868 } },
            { Abilities.Witchcraft.Purge, new uint[] { 11, 4133, 4134, 6078, 6956 } },
            { Abilities.Witchcraft.CourageousAction, new uint[] { 374, 499, 2802, 4700 } },
            { Abilities.Sorcery.InsulatingLens, new uint[] { 95, 426, 427, 428, 429, 1011, 1661, 1760, 1878, 3911, 13802, 13803, 15638, 16870, 16871, 16872, 16927, 16928 } },
            { Abilities.Archery.DoubleRecurve, new uint[] { 451, 452, 453, 454, 7658 } },
            { Abilities.Vitalism.MirrorLight, new uint[] { 370, 552, 15211 } },
            { Abilities.Vitalism.Resurgence, new uint[] { 220, 3455, 6903 } },
            { Abilities.Vitalism.AranzebsBoon, new uint[] { 2955, 2956, 7661, 13790, 13791 } },
            { Abilities.Vitalism.Renewal, new uint[] { 3533, 3534, 3568, 3569, 13797, 13798 } },
            { Abilities.Songcraft.Quickstep, new uint[] { 656, 657, 658, 659, 660, 800, 801, 802, 803, 804, 2183, 2184, 2185, 2186, 2187 } },
            { Abilities.Songcraft.HummingbirdDitty, new uint[] { 462, 463, 464, 465, 466, 13782 } },
            { Abilities.Songcraft.OdeToRecovery, new uint[] { 662, 663, 664, 1024, 2190, 2191, 2192, 4540, 4541, 13783, 13784, 13785, 13786, 13787, 13788 } },
            { Abilities.Songcraft.BulwarkBallad, new uint[] { 778, 1000, 2199, 4386, 4387, 4389 } },
            { Abilities.Songcraft.BloodyChantey, new uint[] { 667, 850, 2196, 7662, 7663, 7664 } },
            { Abilities.Songcraft.GriefsCadence, new uint[] { 6830, 6836, 6844 } },
            { Abilities.Occultism.Urgency, new uint[] { 542, 4252, 7657 } },
            { Abilities.Defense.Refreshment, new uint[] { 53, 331, 332, 333, 334, 1417, 1418, 1419, 1420, 1422, 6081, 7652, 7653, 13620, 13621, 13622, 13623 } },
            { Abilities.Defense.Toughen, new uint[] { 445, 446, 447, 448, 1426, 1427, 1428, 1429, 4535, 13627, 13628, 13629, 13630 } }
        };


        public static SkillProps GetProps(uint id)
        {
            return (skills.ContainsKey(id)) ? skills[id] : new SkillProps();
        }

        public static uint[] GetProdsBuffs(uint id)
        {
            return (producedBuffs.ContainsKey(id)) ? producedBuffs[id] : new uint[0];
        }
    }
}
