using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace ZhiZi.Services
{
    public static class HostageTransferService
    {
        public static void TransferHero(Hero hero, Clan destinationClan, bool addToMainParty)
        {
            if (hero.IsDead || destinationClan == null)
            {
                return;
            }

            if (hero.IsPrisoner)
            {
                EndCaptivityAction.ApplyByReleasedByCompensation(hero);
            }

            Clan oldClan = hero.Clan;

            if (oldClan != destinationClan)
            {
                if (hero.GovernorOf != null)
                {
                    ChangeGovernorAction.RemoveGovernorOf(hero);
                }

                MobileParty? party = hero.PartyBelongedTo;
                if (party != null)
                {
                    if (oldClan != null && oldClan.Kingdom != destinationClan.Kingdom)
                    {
                        if (party.Army != null)
                        {
                            if (party.Army.LeaderParty == party)
                            {
                                DisbandArmyAction.ApplyByUnknownReason(party.Army);
                            }
                            else
                            {
                                party.Army = null;
                            }
                        }

                        IFaction destinationFaction = destinationClan.Kingdom;
                        if (destinationFaction == null)
                        {
                            destinationFaction = destinationClan;
                        }

                        FactionHelper.FinishAllRelatedHostileActionsOfNobleToFaction(
                            hero,
                            destinationFaction);
                    }

                    bool wasPartyLeader = party.LeaderHero == hero;
                    party.MemberRoster.RemoveTroop(
                        hero.CharacterObject,
                        1,
                        default(UniqueTroopDescriptor),
                        0);

                    MakeHeroFugitiveAction.Apply(hero, false);

                    if (wasPartyLeader && party.IsLordParty && !party.IsDisbanding)
                    {
                        DisbandPartyAction.StartDisband(party);
                    }
                }

                hero.Clan = destinationClan;

                if (oldClan != null)
                {
                    foreach (Hero oldClanHero in oldClan.Heroes)
                    {
                        oldClanHero.UpdateHomeSettlement();
                    }
                }

                foreach (Hero newClanHero in destinationClan.Heroes)
                {
                    newClanHero.UpdateHomeSettlement();
                }
            }

            if (addToMainParty && hero.IsAlive && hero.PartyBelongedTo != MobileParty.MainParty)
            {
                AddHeroToPartyAction.Apply(hero, MobileParty.MainParty, false);
            }
        }
    }
}
