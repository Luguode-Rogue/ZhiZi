using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using ZhiZi.Barter;
using ZhiZi.Data;
using ZhiZi.Models;
using ZhiZi.Patches;
using ZhiZi.Services;

namespace ZhiZi.CampaignBehaviors
{
    public sealed class HostageExchangeBehavior : CampaignBehaviorBase
    {
        private const float ContractYears = 2f;

        private List<HostageContract> _contracts = new();

        private Hero? _selectedPlayerHero;
        private Hero? _selectedForeignHero;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                new Action<CampaignGameStarter>(OnSessionLaunched));

            CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                this,
                new Action(OnDailyTick));

            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(
                this,
                new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(
                    OnHeroKilled));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_zhizi_contracts", ref _contracts);
            _contracts ??= new List<HostageContract>();
        }

        public bool IsHeroInActiveContract(Hero hero)
        {
            return _contracts.Any(x => x.Contains(hero));
        }

        public bool IsPlayerSentHostage(Hero hero)
        {
            return _contracts.Any(x => x.PlayerHero == hero);
        }

        public bool CanStartContract(Hero playerHero, Hero foreignHero, Clan foreignClan)
        {
            if (playerHero == null || foreignHero == null || foreignClan == null)
            {
                return false;
            }

            if (foreignClan == Clan.PlayerClan
                || foreignClan.IsEliminated
                || foreignClan.IsBanditFaction
                || foreignClan.IsRebelClan
                || Clan.PlayerClan.IsAtWarWith(foreignClan))
            {
                return false;
            }

            bool playerValid =
                playerHero.IsAlive
                && playerHero.Clan == Clan.PlayerClan
                && playerHero.Age >= 0f
                && playerHero.Age < 18f
                && !playerHero.IsPrisoner
                && !IsHeroInActiveContract(playerHero);

            bool foreignValid =
                foreignHero.IsAlive
                && foreignHero.Clan == foreignClan
                && foreignHero.Age >= 18f
                && foreignHero.Age < 25f
                && foreignHero != foreignClan.Leader
                && foreignHero.Spouse == null
                && !IsHeroInActiveContract(foreignHero);

            return playerValid && foreignValid;
        }

        public void StartContract(Hero playerHero, Hero foreignHero, Clan foreignClan)
        {
            if (!CanStartContract(playerHero, foreignHero, foreignClan))
            {
                return;
            }

            Clan playerOriginalClan = playerHero.Clan;
            Clan foreignOriginalClan = foreignHero.Clan;
            CampaignTime start = CampaignTime.Now;
            CampaignTime end = start + CampaignTime.Years(ContractYears);

            HostageContract contract = new(
                playerHero,
                foreignHero,
                playerOriginalClan,
                foreignOriginalClan,
                start,
                end);

            _contracts.Add(contract);

            HostageTransferService.TransferHero(playerHero, foreignClan, false);
            HostageTransferService.TransferHero(foreignHero, playerOriginalClan, true);

            TextObject message = new TextObject(
                "{=!}质子交换生效：{PLAYER_HERO.NAME}与{FOREIGN_HERO.NAME}交换两年。");

            message.SetCharacterProperties("PLAYER_HERO", playerHero.CharacterObject, false);
            message.SetCharacterProperties("FOREIGN_HERO", foreignHero.CharacterObject, false);
            MBInformationManager.AddQuickInformation(message);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddDialogs(starter);
        }

        private void AddDialogs(CampaignGameStarter starter)
        {
            starter.AddPlayerLine(
                "zhizi_exchange_open",
                "lord_talk_speak_diplomacy_2",
                "zhizi_exchange_player_intro",
                "{=!}我想谈谈两个家族交换质子的事情。",
                CanOpenExchangeConversation,
                BeginExchangeConversation,
                115);

            starter.AddDialogLine(
                "zhizi_exchange_player_intro",
                "zhizi_exchange_player_intro",
                "zhizi_exchange_player_options",
                "{=!}可以。你准备让哪一位年轻家族成员前往我的家族？",
                null,
                null);

            starter.AddRepeatablePlayerLine(
                "zhizi_exchange_player_pick",
                "zhizi_exchange_player_options",
                "zhizi_exchange_foreign_intro",
                "{=!}{HOSTAGE_CANDIDATE.NAME}，{HOSTAGE_AGE}岁。",
                "{=!}看看其他人。",
                "zhizi_exchange_player_options",
                PlayerCandidateCondition,
                SelectPlayerCandidate,
                100);

            starter.AddPlayerLine(
                "zhizi_exchange_player_cancel",
                "zhizi_exchange_player_options",
                "lord_pretalk",
                "{=!}暂且不谈。",
                null,
                ClearSelection,
                120);

            starter.AddDialogLine(
                "zhizi_exchange_foreign_intro",
                "zhizi_exchange_foreign_intro",
                "zhizi_exchange_foreign_options",
                "{=!}那么，你希望我的家族派谁去你那里？",
                null,
                PrepareForeignCandidates);

            starter.AddRepeatablePlayerLine(
                "zhizi_exchange_foreign_pick",
                "zhizi_exchange_foreign_options",
                "zhizi_exchange_confirm",
                "{=!}{HOSTAGE_CANDIDATE.NAME}，{HOSTAGE_AGE}岁{HOSTAGE_STATUS}。",
                "{=!}看看其他人。",
                "zhizi_exchange_foreign_options",
                ForeignCandidateCondition,
                SelectForeignCandidate,
                100);

            starter.AddPlayerLine(
                "zhizi_exchange_foreign_cancel",
                "zhizi_exchange_foreign_options",
                "lord_pretalk",
                "{=!}暂且不谈。",
                null,
                ClearSelection,
                120);

            starter.AddDialogLine(
                "zhizi_exchange_confirm",
                "zhizi_exchange_confirm",
                "zhizi_exchange_confirm_options",
                "{=!}{PLAYER_HERO.NAME}交换{FOREIGN_HERO.NAME}，参考价值约为{ZHIZI_VALUE}第纳尔。接下来谈钱。",
                ConfirmLineCondition,
                null);

            starter.AddPlayerLine(
                "zhizi_exchange_confirm_yes",
                "zhizi_exchange_confirm_options",
                "lord_pretalk",
                "{=!}开始谈条件。",
                ConfirmSelectionCondition,
                StartBarter,
                100);

            starter.AddPlayerLine(
                "zhizi_exchange_confirm_no",
                "zhizi_exchange_confirm_options",
                "lord_pretalk",
                "{=!}还是算了。",
                null,
                ClearSelection,
                120);
        }

        private bool CanOpenExchangeConversation()
        {
            Hero? conversationHero = Hero.OneToOneConversationHero;
            if (conversationHero?.Clan == null
                || conversationHero != conversationHero.Clan.Leader
                || conversationHero.Clan == Clan.PlayerClan
                || conversationHero.Clan.IsEliminated
                || conversationHero.Clan.IsBanditFaction
                || conversationHero.Clan.IsRebelClan
                || Clan.PlayerClan.IsAtWarWith(conversationHero.Clan))
            {
                return false;
            }

            return GetPlayerCandidates().Count > 0
                && GetForeignCandidates(conversationHero.Clan).Count > 0;
        }

        private void BeginExchangeConversation()
        {
            _selectedPlayerHero = null;
            _selectedForeignHero = null;
            ConversationSentence.SetObjectsToRepeatOver(
                GetPlayerCandidates()
                    .Select(x => x.CharacterObject)
                    .ToList(),
                20);
        }

        private bool PlayerCandidateCondition()
        {
            CharacterObject? character =
                ConversationSentence.CurrentProcessedRepeatObject as CharacterObject;

            if (character?.HeroObject == null)
            {
                return false;
            }

            Hero hero = character.HeroObject;
            if (!GetPlayerCandidates().Contains(hero))
            {
                return false;
            }

            StringHelpers.SetRepeatableCharacterProperties(
                "HOSTAGE_CANDIDATE",
                character,
                false);

            MBTextManager.SetTextVariable(
                "HOSTAGE_AGE",
                ((int)hero.Age).ToString(),
                false);

            return true;
        }

        private void SelectPlayerCandidate()
        {
            CharacterObject? character =
                ConversationSentence.SelectedRepeatObject as CharacterObject;

            _selectedPlayerHero = character?.HeroObject;
        }

        private void PrepareForeignCandidates()
        {
            Clan? foreignClan = Hero.OneToOneConversationHero?.Clan;

            if (foreignClan == null)
            {
                return;
            }

            ConversationSentence.SetObjectsToRepeatOver(
                GetForeignCandidates(foreignClan)
                    .Select(x => x.CharacterObject)
                    .ToList(),
                20);
        }

        private bool ForeignCandidateCondition()
        {
            CharacterObject? character =
                ConversationSentence.CurrentProcessedRepeatObject as CharacterObject;

            Clan? foreignClan = Hero.OneToOneConversationHero?.Clan;
            if (character?.HeroObject == null || foreignClan == null)
            {
                return false;
            }

            Hero hero = character.HeroObject;
            if (!GetForeignCandidates(foreignClan).Contains(hero))
            {
                return false;
            }

            StringHelpers.SetRepeatableCharacterProperties(
                "HOSTAGE_CANDIDATE",
                character,
                false);

            MBTextManager.SetTextVariable(
                "HOSTAGE_AGE",
                ((int)hero.Age).ToString(),
                false);

            MBTextManager.SetTextVariable(
                "HOSTAGE_STATUS",
                hero.IsPrisoner ? "（囚犯）" : string.Empty,
                false);

            return true;
        }

        private void SelectForeignCandidate()
        {
            CharacterObject? character =
                ConversationSentence.SelectedRepeatObject as CharacterObject;

            _selectedForeignHero = character?.HeroObject;
        }

        private bool ConfirmLineCondition()
        {
            if (!TryGetCurrentSelection(out Hero playerHero, out Hero foreignHero, out Clan foreignClan))
            {
                return false;
            }

            int requiredGold = HostageExchangeValueModel.CalculateRequiredGold(
                playerHero,
                foreignHero,
                foreignClan);

            MBTextManager.SetTextVariable("PLAYER_HERO", playerHero.Name, false);
            MBTextManager.SetTextVariable("FOREIGN_HERO", foreignHero.Name, false);
            MBTextManager.SetTextVariable("ZHIZI_VALUE", requiredGold, false);
            return true;
        }

        private bool ConfirmSelectionCondition()
        {
            return TryGetCurrentSelection(out _, out _, out _);
        }

        private void StartBarter()
        {
            if (!TryGetCurrentSelection(out Hero playerHero, out Hero foreignHero, out Clan foreignClan))
            {
                ClearSelection();
                return;
            }

            int requiredGold = HostageExchangeValueModel.CalculateRequiredGold(
                playerHero,
                foreignHero,
                foreignClan);

            HostageExchangeBarterable exchange = new(
                Hero.MainHero,
                PartyBase.MainParty,
                playerHero,
                foreignHero,
                foreignClan,
                requiredGold);

            Hero clanLeader = Hero.OneToOneConversationHero;
            MobileParty? leaderParty = clanLeader.PartyBelongedTo;

            BarterManager.Instance.StartBarterOffer(
                Hero.MainHero,
                clanLeader,
                PartyBase.MainParty,
                leaderParty?.Party,
                null,
                InitializeHostageBarterContext,
                0,
                false,
                new Barterable[] { exchange });

            ClearSelection();
        }

        private bool InitializeHostageBarterContext(
            Barterable barterable,
            BarterData args,
            object obj)
        {
            return barterable is HostageExchangeBarterable;
        }

        private bool TryGetCurrentSelection(
            out Hero playerHero,
            out Hero foreignHero,
            out Clan foreignClan)
        {
            playerHero = _selectedPlayerHero!;
            foreignHero = _selectedForeignHero!;
            foreignClan = Hero.OneToOneConversationHero?.Clan!;

            return playerHero != null
                && foreignHero != null
                && foreignClan != null
                && CanStartContract(playerHero, foreignHero, foreignClan);
        }

        private List<Hero> GetPlayerCandidates()
        {
            return Clan.PlayerClan.Heroes
                .Where(x =>
                    x.IsAlive
                    && x.Age >= 0f
                    && x.Age < 18f
                    && !x.IsPrisoner
                    && !IsHeroInActiveContract(x))
                .OrderByDescending(x => x.Age)
                .ToList();
        }

        private List<Hero> GetForeignCandidates(Clan foreignClan)
        {
            return foreignClan.Heroes
                .Where(x =>
                    x.IsAlive
                    && x.Age >= 18f
                    && x.Age < 25f
                    && x != foreignClan.Leader
                    && x.Spouse == null
                    && !IsHeroInActiveContract(x))
                .OrderBy(x => x.Age)
                .ToList();
        }

        private void OnDailyTick()
        {
            foreach (HostageContract contract in _contracts.ToList())
            {
                if (contract.EndTime.IsPast)
                {
                    FinishContract(contract);
                }
            }
        }

        private void OnHeroKilled(
            Hero victim,
            Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail,
            bool showNotification)
        {
            HostageContract? contract =
                _contracts.FirstOrDefault(x => x.Contains(victim));

            if (contract == null)
            {
                return;
            }

            _contracts.Remove(contract);

            if (victim == contract.ForeignHero)
            {
                ReturnPlayerHero(contract);
            }
            else if (victim == contract.PlayerHero)
            {
                ReturnForeignHero(contract);
            }
        }

        private void FinishContract(HostageContract contract)
        {
            if (!_contracts.Remove(contract))
            {
                return;
            }

            ReturnPlayerHero(contract);
            ReturnForeignHero(contract);

            TextObject message = new TextObject(
                "{=!}质子交换期满，双方成员已经归还原家族。");

            MBInformationManager.AddQuickInformation(message);
        }

        private void ReturnPlayerHero(HostageContract contract)
        {
            Hero hero = contract.PlayerHero;
            if (!hero.IsAlive || contract.PlayerOriginalClan.IsEliminated)
            {
                return;
            }

            bool isAdult = hero.Age >= Campaign.Current.Models.AgeModel.HeroComesOfAge;

            HostageTransferService.TransferHero(
                hero,
                contract.PlayerOriginalClan,
                isAdult);

            if (isAdult)
            {
                HostageEducationPatch.RunOriginalCatchUp(hero);
            }
        }

        private void ReturnForeignHero(HostageContract contract)
        {
            Hero hero = contract.ForeignHero;
            if (!hero.IsAlive)
            {
                return;
            }

            if (contract.ForeignOriginalClan.IsEliminated)
            {
                return;
            }

            HostageTransferService.TransferHero(
                hero,
                contract.ForeignOriginalClan,
                false);
        }

        private void ClearSelection()
        {
            _selectedPlayerHero = null;
            _selectedForeignHero = null;
        }
    }
}
