using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;
using ZhiZi.CampaignBehaviors;

namespace ZhiZi.Barter
{
    public sealed class HostageExchangeBarterable : Barterable
    {
        public Hero PlayerHero { get; }
        public Hero ForeignHero { get; }
        public Clan ForeignClan { get; }
        public int RequiredGold { get; }

        public override string StringID => "marriage_barterable";

        public override TextObject Name
        {
            get
            {
                TextObject text = new TextObject(
                    "{=!}质子交换：{PLAYER_HERO.NAME} ↔ {FOREIGN_HERO.NAME}（参考价值 {VALUE}）");

                text.SetCharacterProperties("PLAYER_HERO", PlayerHero.CharacterObject, false);
                text.SetCharacterProperties("FOREIGN_HERO", ForeignHero.CharacterObject, false);
                text.SetTextVariable("VALUE", RequiredGold);
                return text;
            }
        }

        public HostageExchangeBarterable(
            Hero owner,
            PartyBase ownerParty,
            Hero playerHero,
            Hero foreignHero,
            Clan foreignClan,
            int requiredGold)
            : base(owner, ownerParty)
        {
            PlayerHero = playerHero;
            ForeignHero = foreignHero;
            ForeignClan = foreignClan;
            RequiredGold = requiredGold;
        }

        public override int GetUnitValueForFaction(IFaction faction)
        {
            if (faction == ForeignClan)
            {
                return -RequiredGold;
            }

            if (faction == Clan.PlayerClan)
            {
                return RequiredGold;
            }

            return 0;
        }

        public override bool IsCompatible(Barterable barterable)
        {
            if (ReferenceEquals(this, barterable))
            {
                return true;
            }

            return barterable is GoldBarterable && barterable.OriginalOwner == Hero.MainHero;
        }

        public override ImageIdentifier GetVisualIdentifier()
        {
            return new CharacterImageIdentifier(
                CharacterCode.CreateFrom(ForeignHero.CharacterObject));
        }

        public override string GetEncyclopediaLink()
        {
            return ForeignHero.EncyclopediaLink;
        }

        public bool IsStillValid()
        {
            HostageExchangeBehavior? behavior =
                Campaign.Current?.GetCampaignBehavior<HostageExchangeBehavior>();

            return behavior != null
                && behavior.CanStartContract(PlayerHero, ForeignHero, ForeignClan);
        }

        public override void Apply()
        {
            Campaign.Current?.GetCampaignBehavior<HostageExchangeBehavior>()
                ?.StartContract(PlayerHero, ForeignHero, ForeignClan);
        }
    }
}
