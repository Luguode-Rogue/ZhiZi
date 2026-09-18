using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using ZhiZi.CampaignBehaviors;

namespace ZhiZi.Patches
{
    [HarmonyPatch(typeof(DefaultMarriageModel), nameof(DefaultMarriageModel.IsSuitableForMarriage))]
    internal static class HostageMarriagePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Hero maidenOrSuitor, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            HostageExchangeBehavior? behavior =
                Campaign.Current?.GetCampaignBehavior<HostageExchangeBehavior>();

            if (behavior != null && behavior.IsHeroInActiveContract(maidenOrSuitor))
            {
                __result = false;
            }
        }
    }
}
