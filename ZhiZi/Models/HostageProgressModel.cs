using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using ZhiZi.Data;

namespace ZhiZi.Models
{
    public enum HostageTrainingFocus
    {
        Cultural = 0,
        Martial = 1,
        Stewardship = 2,
        Diplomacy = 3
    }

    public static class HostageProgressModel
    {
        public const float PermanentIntegrationRequirement = 80f;
        public const float PermanentYearsRequirement = 3f;
        public const int PermanentRelationRequirement = 50;

        public static float GetMonthlyIntegrationGain(HostageContract contract)
        {
            Hero hero = contract.ForeignHero;
            if (!hero.IsAlive
                || hero.IsPrisoner
                || hero.Clan != contract.PlayerOriginalClan)
            {
                return 0f;
            }

            float gain = 1f;

            if (hero.PartyBelongedTo == MobileParty.MainParty)
            {
                gain += 1f;
            }

            if (hero.GovernorOf != null
                && hero.GovernorOf.OwnerClan == Clan.PlayerClan)
            {
                gain += 1f;
            }

            if (hero.PartyBelongedTo != null
                && hero.PartyBelongedTo.LeaderHero == hero)
            {
                gain += 1f;
            }

            int personalRelation = Hero.MainHero.GetRelation(hero);

            if (personalRelation <= -20)
            {
                gain *= 0.75f;
            }
            else if (personalRelation >= 60)
            {
                gain += 1f;
            }

            return Math.Min(5f, gain);
        }

        public static void ApplyQuarterlyTraining(HostageContract contract, int cycles)
        {
            if (cycles <= 0
                || !contract.PlayerHero.IsAlive
                || contract.PlayerHero.IsPrisoner
                || contract.PlayerHero.Clan != contract.ForeignOriginalClan)
            {
                return;
            }

            Hero hero = contract.PlayerHero;
            string cultureId = contract.ForeignOriginalClan.Culture.StringId;

            SkillObject first;
            SkillObject second;
            SkillObject third;

            switch (cultureId)
            {
                case "vlandia":
                    first = DefaultSkills.Riding;
                    second = DefaultSkills.Polearm;
                    third = DefaultSkills.Leadership;
                    break;
                case "battania":
                    first = DefaultSkills.Bow;
                    second = DefaultSkills.Scouting;
                    third = DefaultSkills.Athletics;
                    break;
                case "empire":
                    first = DefaultSkills.Steward;
                    second = DefaultSkills.Charm;
                    third = DefaultSkills.Engineering;
                    break;
                case "khuzait":
                    first = DefaultSkills.Riding;
                    second = DefaultSkills.Bow;
                    third = DefaultSkills.Scouting;
                    break;
                case "sturgia":
                    first = DefaultSkills.TwoHanded;
                    second = DefaultSkills.Throwing;
                    third = DefaultSkills.Athletics;
                    break;
                case "aserai":
                    first = DefaultSkills.Trade;
                    second = DefaultSkills.Riding;
                    third = DefaultSkills.Steward;
                    break;
                default:
                    first = DefaultSkills.Charm;
                    second = DefaultSkills.Leadership;
                    third = DefaultSkills.Steward;
                    break;
            }

            float ageFactor = Math.Max(
                0.25f,
                Math.Min(1f, hero.Age / 12f));

            hero.AddSkillXp(first, 220f * cycles * ageFactor);
            hero.AddSkillXp(second, 170f * cycles * ageFactor);
            hero.AddSkillXp(third, 130f * cycles * ageFactor);

            HostageTrainingFocus focus = (HostageTrainingFocus)contract.TrainingFocus;
            switch (focus)
            {
                case HostageTrainingFocus.Martial:
                    hero.AddSkillXp(DefaultSkills.OneHanded, 180f * cycles * ageFactor);
                    hero.AddSkillXp(DefaultSkills.Athletics, 140f * cycles * ageFactor);
                    hero.AddSkillXp(DefaultSkills.Leadership, 120f * cycles * ageFactor);
                    break;
                case HostageTrainingFocus.Stewardship:
                    hero.AddSkillXp(DefaultSkills.Steward, 200f * cycles * ageFactor);
                    hero.AddSkillXp(DefaultSkills.Trade, 140f * cycles * ageFactor);
                    hero.AddSkillXp(DefaultSkills.Engineering, 100f * cycles * ageFactor);
                    break;
                case HostageTrainingFocus.Diplomacy:
                    hero.AddSkillXp(DefaultSkills.Charm, 220f * cycles * ageFactor);
                    hero.AddSkillXp(DefaultSkills.Leadership, 140f * cycles * ageFactor);
                    hero.AddSkillXp(DefaultSkills.Trade, 100f * cycles * ageFactor);
                    break;
            }
        }

        public static bool CanMakePermanent(
            HostageContract contract,
            out string reason)
        {
            if (!contract.PlayerHero.IsAlive || !contract.ForeignHero.IsAlive)
            {
                reason = "交换双方必须都存活。";
                return false;
            }

            if (contract.PlayerOriginalClan.IsEliminated
                || contract.ForeignOriginalClan.IsEliminated)
            {
                reason = "一方家族已经覆灭，无法签署永久交换。";
                return false;
            }

            if (contract.PlayerHero.Clan != contract.ForeignOriginalClan
                || contract.ForeignHero.Clan != contract.PlayerOriginalClan)
            {
                reason = "双方当前Clan归属已经与质子约定不一致。";
                return false;
            }

            if (FactionManager.IsAtWarAgainstFaction(
                contract.PlayerOriginalClan,
                contract.ForeignOriginalClan))
            {
                reason = "交战期间不能签署永久交换。";
                return false;
            }

            Hero leader = contract.ForeignOriginalClan.Leader;
            if (leader == null || !leader.IsAlive)
            {
                reason = "对方当前没有可签署永久交换的Clan Leader。";
                return false;
            }

            float elapsedDays = contract.StartTime.ElapsedDaysUntilNow;
            int personalRelation = Hero.MainHero.GetRelation(contract.ForeignHero);
            int leaderRelation = Hero.MainHero.GetRelation(leader);

            if (contract.DevotedToPlayerClan)
            {
                if (elapsedDays < CampaignTime.Years(2f).ToDays)
                {
                    reason = "质子的效忠仍需要至少两年的共同经历。";
                    return false;
                }

                if (leaderRelation < 20)
                {
                    reason = "与对方当前Clan Leader的关系需要达到20。";
                    return false;
                }

                reason = string.Empty;
                return true;
            }

            if (contract.WantsToStay)
            {
                if (elapsedDays < CampaignTime.Years(2f).ToDays)
                {
                    reason = "质子主动留下仍要求至少履约两年。";
                    return false;
                }

                if (contract.IntegrationProgress < 60f)
                {
                    reason = "主动留下需要整合度达到60。";
                    return false;
                }

                if (personalRelation < 80)
                {
                    reason = "质子当前与你的关系需要达到80。";
                    return false;
                }

                if (leaderRelation < 30)
                {
                    reason = "与对方当前Clan Leader的关系需要达到30。";
                    return false;
                }

                reason = string.Empty;
                return true;
            }

            if (elapsedDays < CampaignTime.Years(PermanentYearsRequirement).ToDays)
            {
                reason = "至少需要维持三年质子关系。";
                return false;
            }

            if (contract.IntegrationProgress < PermanentIntegrationRequirement)
            {
                reason = "外来质子的整合度需要达到80。";
                return false;
            }

            if (leaderRelation < PermanentRelationRequirement)
            {
                reason = "与对方当前Clan Leader的关系需要达到50。";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static string GetIntegrationDescription(float progress)
        {
            if (progress < 20f)
            {
                return "陌生";
            }

            if (progress < 40f)
            {
                return "适应";
            }

            if (progress < 60f)
            {
                return "亲近";
            }

            if (progress < 80f)
            {
                return "忠诚";
            }

            return "视若家人";
        }
    }
}
