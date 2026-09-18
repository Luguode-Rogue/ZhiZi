using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using ZhiZi.CampaignBehaviors;

namespace ZhiZi.Patches
{
    [HarmonyPatch(typeof(EducationCampaignBehavior), "OnHeroComesOfAge")]
    internal static class HostageEducationPatch
    {
        private static readonly MethodInfo? OriginalMethod =
            AccessTools.Method(typeof(EducationCampaignBehavior), "OnHeroComesOfAge");

        [HarmonyPrefix]
        private static bool Prefix(Hero hero)
        {
            HostageExchangeBehavior? behavior =
                Campaign.Current?.GetCampaignBehavior<HostageExchangeBehavior>();

            return behavior == null || !behavior.IsPlayerSentHostage(hero);
        }

        public static void RunOriginalCatchUp(Hero hero)
        {
            EducationCampaignBehavior? educationBehavior =
                Campaign.Current?.GetCampaignBehavior<EducationCampaignBehavior>();

            if (educationBehavior == null || OriginalMethod == null || hero.IsDead)
            {
                return;
            }

            OriginalMethod.Invoke(educationBehavior, new object[] { hero });
        }
    }
}
