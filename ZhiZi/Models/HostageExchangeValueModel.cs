using System;
using TaleWorlds.CampaignSystem;

namespace ZhiZi.Models
{
    public static class HostageExchangeValueModel
    {
        private const int MinimumValue = 5_000;
        private const int MaximumValue = 500_000;

        public static int CalculateRequiredGold(Hero playerMinor, Hero foreignHero, Clan foreignClan)
        {
            Clan playerClan = Clan.PlayerClan;

            float foreignHeroValue = Campaign.Current.Models.DiplomacyModel
                .GetValueOfHeroForFaction(foreignHero, foreignClan, true);

            float playerHeroValue = Campaign.Current.Models.DiplomacyModel
                .GetValueOfHeroForFaction(playerMinor, playerClan, true);

            float foreignAgePremium = Math.Max(0f, 24f - foreignHero.Age) * 2500f;

            float playerAgeRatio = Math.Max(0f, Math.Min(17f, playerMinor.Age)) / 17f;
            float maturity = 0.15f + 0.85f * (float)Math.Pow(playerAgeRatio, 1.5d);
            float playerCredit = playerHeroValue * maturity + playerAgeRatio * 20_000f;

            float tierDifference = GetClanRank(foreignClan) - GetClanRank(playerClan);
            float tierAdjustment = tierDifference * Math.Abs(tierDifference) * 1000f;

            int personalRelation = playerMinor.GetRelation(foreignHero);
            int clanRelation = FactionManager.GetRelationBetweenClans(playerClan, foreignClan);
            float relationAdjustment = -(personalRelation + clanRelation) * 250f;

            float crossKingdomPremium = foreignClan.Kingdom != playerClan.Kingdom
                ? foreignHeroValue
                : 0f;

            float prisonerPremium = foreignHero.IsPrisoner
                ? 10_000f + foreignHeroValue * 0.15f
                : 0f;

            float rawValue =
                50_000f
                + foreignHeroValue
                + foreignAgePremium
                + tierAdjustment
                + crossKingdomPremium
                + prisonerPremium
                + foreignClan.Renown
                + relationAdjustment
                - playerCredit;

            int rounded = (int)(Math.Round(rawValue / 100d) * 100d);
            return Math.Max(MinimumValue, Math.Min(MaximumValue, rounded));
        }

        public static float CalculateSuccessChance(int paidGold, int requiredGold)
        {
            if (requiredGold <= 0)
            {
                return 1f;
            }

            float ratio = paidGold / (float)requiredGold;

            if (ratio < 0.25f)
            {
                return 0f;
            }

            if (ratio >= 1f)
            {
                return 1f;
            }

            if (ratio <= 0.50f)
            {
                return Lerp(0.10f, 0.35f, (ratio - 0.25f) / 0.25f);
            }

            if (ratio <= 0.75f)
            {
                return Lerp(0.35f, 0.70f, (ratio - 0.50f) / 0.25f);
            }

            return Lerp(0.70f, 1f, (ratio - 0.75f) / 0.25f);
        }

        private static float GetClanRank(Clan clan)
        {
            float value = clan.Tier;

            if (clan.MapFaction != null && clan.Leader == clan.MapFaction.Leader)
            {
                value += Math.Min(3f, clan.MapFaction.Fiefs.Count / 10f) + 0.5f;
            }

            return value;
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * Math.Max(0f, Math.Min(1f, t));
        }
    }
}
