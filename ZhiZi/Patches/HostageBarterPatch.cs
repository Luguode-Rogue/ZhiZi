using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using ZhiZi.Barter;
using ZhiZi.Models;

namespace ZhiZi.Patches
{
    [HarmonyPatch]
    internal static class HostageBarterPatch
    {
        [HarmonyPatch(typeof(BarterManager), nameof(BarterManager.IsOfferAcceptable))]
        [HarmonyPostfix]
        private static void IsOfferAcceptablePostfix(BarterData args, ref bool __result)
        {
            HostageExchangeBarterable? exchange = FindExchange(args);
            if (exchange == null)
            {
                return;
            }

            int paidGold = GetPlayerGoldOffer(args);
            int minimumBid = (int)Math.Ceiling(exchange.RequiredGold * 0.25f);
            __result = exchange.IsStillValid() && paidGold >= minimumBid;
        }

        [HarmonyPatch(typeof(BarterManager), nameof(BarterManager.ApplyAndFinalizePlayerBarter))]
        [HarmonyPrefix]
        private static bool ApplyAndFinalizePrefix(
            BarterManager __instance,
            Hero offererHero,
            Hero otherHero,
            BarterData barterData)
        {
            HostageExchangeBarterable? exchange = FindExchange(barterData);
            if (exchange == null)
            {
                return true;
            }

            if (!exchange.IsStillValid())
            {
                __instance.CancelAndFinalizePlayerBarter(offererHero, otherHero, barterData);
                return false;
            }

            int paidGold = GetPlayerGoldOffer(barterData);
            float chance = HostageExchangeValueModel.CalculateSuccessChance(
                paidGold,
                exchange.RequiredGold);

            if (MBRandom.RandomFloat > chance)
            {
                __instance.CancelAndFinalizePlayerBarter(offererHero, otherHero, barterData);
                return false;
            }

            AccessTools.Field(typeof(BarterManager), "_overpayAmount")?.SetValue(__instance, 0f);
            return true;
        }

        private static HostageExchangeBarterable? FindExchange(BarterData barterData)
        {
            return barterData.GetOfferedBarterables()
                .OfType<HostageExchangeBarterable>()
                .FirstOrDefault();
        }

        private static int GetPlayerGoldOffer(BarterData barterData)
        {
            return barterData.GetOfferedBarterables()
                .OfType<GoldBarterable>()
                .Where(x => x.OriginalOwner == Hero.MainHero)
                .Sum(x => x.CurrentAmount);
        }
    }
}
