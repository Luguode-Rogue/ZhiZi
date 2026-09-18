using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.MapEvents;
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
        private const float InitialContractYears = 2f;
        private const float IntegrationUpdateDays = 30f;
        private const float TrainingUpdateDays = 90f;

        private List<HostageContract> _contracts = new();

        private Hero? _selectedPlayerHero;
        private Hero? _selectedForeignHero;
        private HostageContract? _selectedManagedContract;

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

            CampaignEvents.WarDeclared.AddNonSerializedListener(
                this,
                new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(
                    OnWarDeclared));

            CampaignEvents.MapEventEnded.AddNonSerializedListener(
                this,
                new Action<MapEvent>(OnMapEventEnded));

            CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(
                this,
                new Action<Clan>(OnClanDestroyed));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_zhizi_contracts", ref _contracts);
            _contracts ??= new List<HostageContract>();

            foreach (HostageContract contract in _contracts)
            {
                contract.EnsureInitialized();
            }
        }

        public bool IsHeroInActiveContract(Hero hero)
        {
            return _contracts.Any(x => x.Contains(hero));
        }

        public bool IsPlayerSentHostage(Hero hero)
        {
            return _contracts.Any(x => x.PlayerHero == hero);
        }

        public HostageContract? GetContractForHero(Hero hero)
        {
            return _contracts.FirstOrDefault(x => x.Contains(hero));
        }

        public bool CanStartContract(Hero playerHero, Hero foreignHero, Clan foreignClan)
        {
            if (playerHero == null || foreignHero == null || foreignClan == null)
            {
                return false;
            }

            int contractLimit = Math.Max(1, Clan.PlayerClan.Tier + 1);
            if (_contracts.Count >= contractLimit)
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

            Hero foreignLeader = foreignClan.Leader;
            if (foreignLeader == null)
            {
                return false;
            }

            int minimumRelation =
                foreignClan.Kingdom != null
                && foreignClan.Kingdom == Clan.PlayerClan.Kingdom
                    ? 0
                    : 10;

            if (Hero.MainHero.GetRelation(foreignLeader) < minimumRelation)
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
            CampaignTime end = start + CampaignTime.Years(InitialContractYears);

            HostageContract contract = new(
                playerHero,
                foreignHero,
                playerOriginalClan,
                foreignOriginalClan,
                start,
                end);

            _contracts.Add(contract);

            HostageTransferService.TransferHero(
                playerHero,
                foreignOriginalClan,
                false);

            HostageTransferService.TransferHero(
                foreignHero,
                playerOriginalClan,
                true);

            ChangeForeignClanRelation(contract, 3, true);

            TextObject message = new TextObject(
                "{=!}质子交换生效：{PLAYER_HERO.NAME}与{FOREIGN_HERO.NAME}交换两年。");

            message.SetCharacterProperties(
                "PLAYER_HERO",
                playerHero.CharacterObject,
                false);

            message.SetCharacterProperties(
                "FOREIGN_HERO",
                foreignHero.CharacterObject,
                false);

            MBInformationManager.AddQuickInformation(message);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddExchangeDialogs(starter);
            AddContractManagementDialogs(starter);
        }

        private void AddExchangeDialogs(CampaignGameStarter starter)
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
                ClearExchangeSelection,
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
                ClearExchangeSelection,
                120);

            starter.AddDialogLine(
                "zhizi_exchange_confirm",
                "zhizi_exchange_confirm",
                "zhizi_exchange_confirm_options",
                "{=!}{PLAYER_HERO.NAME}交换{FOREIGN_HERO.NAME}，参考价值约为{ZHIZI_VALUE}第纳尔。报价至少达到参考价值的25%才能提交，足额报价必定成功。",
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
                ClearExchangeSelection,
                120);
        }

        private void AddContractManagementDialogs(CampaignGameStarter starter)
        {
            starter.AddPlayerLine(
                "zhizi_manage_open",
                "lord_talk_speak_diplomacy_2",
                "zhizi_manage_intro",
                "{=!}我想谈谈我们现有的质子约定。",
                CanOpenContractManagement,
                BeginContractManagement,
                114);

            starter.AddDialogLine(
                "zhizi_manage_intro",
                "zhizi_manage_intro",
                "zhizi_manage_contract_list",
                "{=!}当然。你想处理哪一份约定？",
                null,
                null);

            starter.AddRepeatablePlayerLine(
                "zhizi_manage_contract_pick",
                "zhizi_manage_contract_list",
                "zhizi_manage_detail",
                "{=!}{ZHIZI_PLAYER_NAME} ↔ {ZHIZI_FOREIGN_NAME}，整合度{ZHIZI_INTEGRATION}。",
                "{=!}看看其他约定。",
                "zhizi_manage_contract_list",
                ManagedContractCandidateCondition,
                SelectManagedContract,
                100);

            starter.AddPlayerLine(
                "zhizi_manage_contract_cancel",
                "zhizi_manage_contract_list",
                "lord_pretalk",
                "{=!}暂且不谈。",
                null,
                ClearManagedContract,
                120);

            starter.AddDialogLine(
                "zhizi_manage_detail",
                "zhizi_manage_detail",
                "zhizi_manage_options",
                "{=!}{ZHIZI_PLAYER_NAME}与{ZHIZI_FOREIGN_NAME}的约定还剩约{ZHIZI_REMAINING_DAYS}天。整合度为{ZHIZI_INTEGRATION}（{ZHIZI_INTEGRATION_DESC}）。",
                ManagedContractDetailsCondition,
                null);

            starter.AddPlayerLine(
                "zhizi_manage_recall",
                "zhizi_manage_options",
                "lord_pretalk",
                "{=!}我要提前召回双方质子。",
                SelectedManagedContractCondition,
                RecallSelectedContract,
                100);

            starter.AddPlayerLine(
                "zhizi_manage_renew",
                "zhizi_manage_options",
                "lord_pretalk",
                "{=!}把约定再延长一年。",
                SelectedManagedContractCondition,
                RenewSelectedContract,
                100,
                RenewSelectedContractClickable);

            starter.AddPlayerLine(
                "zhizi_manage_permanent",
                "zhizi_manage_options",
                "lord_pretalk",
                "{=!}让这次交换永久生效。",
                SelectedManagedContractCondition,
                MakeSelectedContractPermanent,
                100,
                PermanentSelectedContractClickable);

            starter.AddPlayerLine(
                "zhizi_manage_back",
                "zhizi_manage_options",
                "lord_pretalk",
                "{=!}先这样吧。",
                null,
                ClearManagedContract,
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

            int contractLimit = Math.Max(1, Clan.PlayerClan.Tier + 1);
            if (_contracts.Count >= contractLimit)
            {
                return false;
            }

            int minimumRelation =
                conversationHero.Clan.Kingdom != null
                && conversationHero.Clan.Kingdom == Clan.PlayerClan.Kingdom
                    ? 0
                    : 10;

            if (Hero.MainHero.GetRelation(conversationHero) < minimumRelation)
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
                    .Select(x => (object)x.CharacterObject)
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
                    .Select(x => (object)x.CharacterObject)
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
            if (!TryGetCurrentSelection(
                out Hero playerHero,
                out Hero foreignHero,
                out Clan foreignClan))
            {
                return false;
            }

            int requiredGold = HostageExchangeValueModel.CalculateRequiredGold(
                playerHero,
                foreignHero,
                foreignClan);

            MBTextManager.SetTextVariable(
                "PLAYER_HERO",
                playerHero.Name,
                false);

            MBTextManager.SetTextVariable(
                "FOREIGN_HERO",
                foreignHero.Name,
                false);

            MBTextManager.SetTextVariable("ZHIZI_VALUE", requiredGold);
            return true;
        }

        private bool ConfirmSelectionCondition()
        {
            return TryGetCurrentSelection(out _, out _, out _);
        }

        private void StartBarter()
        {
            if (!TryGetCurrentSelection(
                out Hero playerHero,
                out Hero foreignHero,
                out Clan foreignClan))
            {
                ClearExchangeSelection();
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

            ClearExchangeSelection();
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
                    && !IsHeroInActiveContract(x))
                .OrderBy(x => x.Age)
                .ToList();
        }

        private bool CanOpenContractManagement()
        {
            Hero? conversationHero = Hero.OneToOneConversationHero;
            return conversationHero?.Clan != null
                && conversationHero == conversationHero.Clan.Leader
                && GetContractsForClan(conversationHero.Clan).Count > 0;
        }

        private void BeginContractManagement()
        {
            _selectedManagedContract = null;

            ConversationSentence.SetObjectsToRepeatOver(
                GetContractsForClan(Hero.OneToOneConversationHero.Clan)
                    .Select(x => (object)x)
                    .ToList(),
                20);
        }

        private List<HostageContract> GetContractsForClan(Clan clan)
        {
            return _contracts
                .Where(x => x.ForeignOriginalClan == clan)
                .OrderBy(x => x.EndTime.RemainingDaysFromNow)
                .ToList();
        }

        private bool ManagedContractCandidateCondition()
        {
            HostageContract? contract =
                ConversationSentence.CurrentProcessedRepeatObject as HostageContract;

            if (contract == null || !_contracts.Contains(contract))
            {
                return false;
            }

            SetManagedContractTextVariables(contract);
            return true;
        }

        private void SelectManagedContract()
        {
            _selectedManagedContract =
                ConversationSentence.SelectedRepeatObject as HostageContract;
        }

        private bool ManagedContractDetailsCondition()
        {
            if (!SelectedManagedContractCondition())
            {
                return false;
            }

            SetManagedContractTextVariables(_selectedManagedContract!);
            return true;
        }

        private bool SelectedManagedContractCondition()
        {
            HostageContract? contract = _selectedManagedContract;
            Hero? conversationHero = Hero.OneToOneConversationHero;

            return contract != null
                && _contracts.Contains(contract)
                && conversationHero?.Clan == contract.ForeignOriginalClan
                && conversationHero == conversationHero.Clan.Leader;
        }

        private void SetManagedContractTextVariables(HostageContract contract)
        {
            MBTextManager.SetTextVariable(
                "ZHIZI_PLAYER_NAME",
                contract.PlayerHero.Name,
                false);

            MBTextManager.SetTextVariable(
                "ZHIZI_FOREIGN_NAME",
                contract.ForeignHero.Name,
                false);

            MBTextManager.SetTextVariable(
                "ZHIZI_INTEGRATION",
                (int)contract.IntegrationProgress);

            MBTextManager.SetTextVariable(
                "ZHIZI_INTEGRATION_DESC",
                HostageProgressModel.GetIntegrationDescription(
                    contract.IntegrationProgress),
                false);

            int remainingDays = Math.Max(
                0,
                (int)Math.Ceiling(contract.EndTime.RemainingDaysFromNow));

            MBTextManager.SetTextVariable(
                "ZHIZI_REMAINING_DAYS",
                remainingDays);
        }

        private bool RenewSelectedContractClickable(out TextObject hintText)
        {
            HostageContract? contract = _selectedManagedContract;
            if (contract == null || !_contracts.Contains(contract))
            {
                hintText = new TextObject("{=!}没有有效的质子约定。");
                return false;
            }

            if (!contract.CanRenew(1f))
            {
                hintText = new TextObject("{=!}质子约定总期限不能超过十年。");
                return false;
            }

            hintText = TextObject.GetEmpty();
            return true;
        }

        private bool PermanentSelectedContractClickable(out TextObject hintText)
        {
            HostageContract? contract = _selectedManagedContract;
            if (contract == null || !_contracts.Contains(contract))
            {
                hintText = new TextObject("{=!}没有有效的质子约定。");
                return false;
            }

            if (!HostageProgressModel.CanMakePermanent(contract, out string reason))
            {
                hintText = new TextObject("{=!}" + reason);
                return false;
            }

            hintText = TextObject.GetEmpty();
            return true;
        }

        private void RecallSelectedContract()
        {
            HostageContract? contract = _selectedManagedContract;
            ClearManagedContract();

            if (contract == null || !_contracts.Contains(contract))
            {
                return;
            }

            float elapsedDays = contract.StartTime.ElapsedDaysUntilNow;
            bool veryEarly = elapsedDays < 90f;
            int relationPenalty = veryEarly ? -15 : -8;
            float influenceCost = veryEarly ? 25f : 10f;

            ChangeForeignClanRelation(contract, relationPenalty, true);

            float actualInfluenceCost = Math.Min(
                influenceCost,
                Math.Max(0f, Clan.PlayerClan.Influence));

            if (actualInfluenceCost > 0f)
            {
                ChangeClanInfluenceAction.Apply(
                    Clan.PlayerClan,
                    -actualInfluenceCost);
            }

            FinishContract(
                contract,
                ContractEndReason.EarlyRecall,
                true);
        }

        private void RenewSelectedContract()
        {
            HostageContract? contract = _selectedManagedContract;
            ClearManagedContract();

            if (contract != null)
            {
                RenewContract(contract);
            }
        }

        private void MakeSelectedContractPermanent()
        {
            HostageContract? contract = _selectedManagedContract;
            ClearManagedContract();

            if (contract != null)
            {
                MakePermanent(contract);
            }
        }

        private void ClearManagedContract()
        {
            _selectedManagedContract = null;
        }

        private void OnDailyTick()
        {
            foreach (HostageContract contract in _contracts.ToList())
            {
                contract.EnsureInitialized();

                if (AreContractClansAtWar(contract))
                {
                    FinishContract(
                        contract,
                        ContractEndReason.War,
                        true);
                    continue;
                }

                if (contract.ForeignOriginalClan.IsEliminated)
                {
                    HandleForeignClanDestroyed(contract);
                    continue;
                }

                if (HandleSuccessionRecall(contract))
                {
                    continue;
                }

                UpdateIntegration(contract);
                UpdateTraining(contract);
            }

            TryShowExpiredContractInquiry();

            if (!InformationManager.IsAnyInquiryActive())
            {
                TryShowTrainingFocusInquiry();
            }
        }

        private void UpdateIntegration(HostageContract contract)
        {
            int periods = (int)(
                contract.LastIntegrationUpdate.ElapsedDaysUntilNow
                / IntegrationUpdateDays);

            if (periods <= 0)
            {
                return;
            }

            float oldProgress = contract.IntegrationProgress;
            float monthlyGain =
                HostageProgressModel.GetMonthlyIntegrationGain(contract);

            contract.AddIntegration(monthlyGain * periods);
            contract.MarkIntegrationUpdated();

            HandleIntegrationThresholds(
                contract,
                oldProgress,
                contract.IntegrationProgress);
        }

        private void UpdateTraining(HostageContract contract)
        {
            if (!contract.PlayerHero.IsAlive
                || contract.PlayerHero.IsPrisoner)
            {
                return;
            }

            int periods = (int)(
                contract.LastTrainingUpdate.ElapsedDaysUntilNow
                / TrainingUpdateDays);

            if (periods <= 0)
            {
                return;
            }

            HostageProgressModel.ApplyQuarterlyTraining(
                contract,
                periods);

            contract.MarkTrainingUpdated();
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            foreach (HostageContract contract in _contracts.ToList())
            {
                Hero foreignHero = contract.ForeignHero;
                if (!foreignHero.IsAlive)
                {
                    continue;
                }

                PartyBase? heroParty = foreignHero.PartyBelongedTo?.Party;
                if (heroParty == null
                    || !mapEvent.InvolvedParties.Any(x => x == heroParty))
                {
                    continue;
                }

                float oldProgress = contract.IntegrationProgress;
                float gain = contract.HasBattleExperience ? 1f : 4f;

                contract.AddIntegration(gain);

                if (!contract.HasBattleExperience)
                {
                    contract.MarkBattleExperience();

                    TextObject firstBattle = new TextObject(
                        "{=!}{FOREIGN_HERO.NAME}第一次以你家族成员的身份经历了战斗，整合度明显提升。");

                    firstBattle.SetCharacterProperties(
                        "FOREIGN_HERO",
                        foreignHero.CharacterObject,
                        false);

                    MBInformationManager.AddQuickInformation(firstBattle);
                }

                HandleIntegrationThresholds(
                    contract,
                    oldProgress,
                    contract.IntegrationProgress);
            }
        }

        private void HandleIntegrationThresholds(
            HostageContract contract,
            float oldProgress,
            float newProgress)
        {
            int[] thresholds = { 20, 40, 60, 80, 100 };

            foreach (int threshold in thresholds)
            {
                if (oldProgress >= threshold || newProgress < threshold)
                {
                    continue;
                }

                ChangeForeignClanRelation(contract, 1, false);

                TextObject message = new TextObject(
                    "{=!}{FOREIGN_HERO.NAME}的整合度达到{ZHIZI_INTEGRATION}：{ZHIZI_INTEGRATION_DESC}。");

                message.SetCharacterProperties(
                    "FOREIGN_HERO",
                    contract.ForeignHero.CharacterObject,
                    false);

                message.SetTextVariable(
                    "ZHIZI_INTEGRATION",
                    threshold);

                message.SetTextVariable(
                    "ZHIZI_INTEGRATION_DESC",
                    HostageProgressModel.GetIntegrationDescription(threshold));

                MBInformationManager.AddQuickInformation(message);
            }
        }

        private void OnWarDeclared(
            IFaction faction1,
            IFaction faction2,
            DeclareWarAction.DeclareWarDetail detail)
        {
            foreach (HostageContract contract in _contracts.ToList())
            {
                if (AreContractClansAtWar(contract))
                {
                    FinishContract(
                        contract,
                        ContractEndReason.War,
                        true);
                }
            }
        }

        private bool AreContractClansAtWar(HostageContract contract)
        {
            return FactionManager.IsAtWarAgainstFaction(
                contract.PlayerOriginalClan,
                contract.ForeignOriginalClan);
        }

        private bool HandleSuccessionRecall(HostageContract contract)
        {
            if (contract.SuccessionResolved)
            {
                return false;
            }

            Hero? originalLeader = contract.ForeignLeaderAtStart;
            if (originalLeader == null || originalLeader.IsAlive)
            {
                return false;
            }

            Hero? currentLeader = contract.ForeignOriginalClan.Leader;
            if (currentLeader == null
                || currentLeader == originalLeader
                || !currentLeader.IsAlive)
            {
                return false;
            }

            contract.ResolveSuccession();

            if (contract.IntegrationProgress >= 60f)
            {
                TextObject continued = new TextObject(
                    "{=!}{FOREIGN_CLAN}更换了Clan Leader，但由于{FOREIGN_HERO.NAME}已经与玩家家族关系密切，新任领袖同意继续质子约定。");

                continued.SetTextVariable(
                    "FOREIGN_CLAN",
                    contract.ForeignOriginalClan.Name);

                continued.SetCharacterProperties(
                    "FOREIGN_HERO",
                    contract.ForeignHero.CharacterObject,
                    false);

                MBInformationManager.AddQuickInformation(continued);
                return false;
            }

            FinishContract(
                contract,
                ContractEndReason.SuccessionRecall,
                true);

            return true;
        }

        private void OnClanDestroyed(Clan destroyedClan)
        {
            foreach (HostageContract contract in _contracts.ToList())
            {
                if (contract.ForeignOriginalClan == destroyedClan)
                {
                    HandleForeignClanDestroyed(contract);
                }
                else if (contract.PlayerOriginalClan == destroyedClan)
                {
                    _contracts.Remove(contract);
                    ReturnForeignHero(contract);
                }
            }
        }

        private void HandleForeignClanDestroyed(HostageContract contract)
        {
            if (!_contracts.Remove(contract))
            {
                return;
            }

            ReturnPlayerHero(contract);

            TextObject message = new TextObject(
                "{=!}{FOREIGN_CLAN}已经覆灭。你的家族成员被召回，而{FOREIGN_HERO.NAME}因为失去原家族而继续留在你的Clan。");

            message.SetTextVariable(
                "FOREIGN_CLAN",
                contract.ForeignOriginalClan.Name);

            message.SetCharacterProperties(
                "FOREIGN_HERO",
                contract.ForeignHero.CharacterObject,
                false);

            MBInformationManager.AddQuickInformation(message);
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
                int relationPenalty = killer == Hero.MainHero ? -30 : -20;
                ChangeForeignClanRelation(
                    contract,
                    relationPenalty,
                    true);

                ReturnPlayerHero(contract);

                TextObject message = new TextObject(
                    "{=!}{FOREIGN_HERO.NAME}在质子期间死亡，约定立即终止。");

                message.SetCharacterProperties(
                    "FOREIGN_HERO",
                    contract.ForeignHero.CharacterObject,
                    false);

                MBInformationManager.AddQuickInformation(message);
            }
            else if (victim == contract.PlayerHero)
            {
                ReturnForeignHero(contract);

                MBInformationManager.AddQuickInformation(
                    new TextObject(
                        "{=!}你送出的家族成员在质子期间死亡，幸存的对方质子已经被归还。"));
            }
        }

        private void TryShowExpiredContractInquiry()
        {
            if (InformationManager.IsAnyInquiryActive())
            {
                return;
            }

            HostageContract? contract =
                _contracts.FirstOrDefault(x => x.EndTime.IsPast);

            if (contract == null)
            {
                return;
            }

            bool canRenew = contract.CanRenew(1f);
            bool canPermanent =
                HostageProgressModel.CanMakePermanent(
                    contract,
                    out string permanentReason);

            List<InquiryElement> options = new()
            {
                new InquiryElement(
                    "return",
                    "按期归还双方质子",
                    null,
                    true,
                    "结束临时约定，双方成员返回原Clan。"),

                new InquiryElement(
                    "renew",
                    "续约一年",
                    null,
                    canRenew,
                    canRenew
                        ? "维持当前交换状态一年。"
                        : "质子约定总期限不能超过十年。"),

                new InquiryElement(
                    "permanent",
                    "永久交换",
                    null,
                    canPermanent,
                    canPermanent
                        ? "双方保留当前Clan归属，临时约定结束。"
                        : permanentReason)
            };

            string description =
                contract.PlayerHero.Name
                + " ↔ "
                + contract.ForeignHero.Name
                + "\n整合度："
                + ((int)contract.IntegrationProgress)
                + "（"
                + HostageProgressModel.GetIntegrationDescription(
                    contract.IntegrationProgress)
                + "）";

            MultiSelectionInquiryData inquiry = new(
                "质子交换到期",
                description,
                options,
                false,
                1,
                1,
                "确定",
                string.Empty,
                selected => HandleExpiryDecision(contract, selected),
                null,
                string.Empty,
                false);

            MBInformationManager.ShowMultiSelectionInquiry(
                inquiry,
                true,
                true);
        }

        private void HandleExpiryDecision(
            HostageContract contract,
            List<InquiryElement> selected)
        {
            if (!_contracts.Contains(contract)
                || selected == null
                || selected.Count == 0)
            {
                return;
            }

            string? choice = selected[0].Identifier as string;
            switch (choice)
            {
                case "renew":
                    RenewContract(contract);
                    break;
                case "permanent":
                    MakePermanent(contract);
                    break;
                default:
                    FinishContract(
                        contract,
                        ContractEndReason.NormalExpiry,
                        true);
                    break;
            }
        }

        private void TryShowTrainingFocusInquiry()
        {
            HostageContract? contract = _contracts.FirstOrDefault(x =>
                !x.EndTime.IsPast
                && x.PlayerHero.IsAlive
                && x.PlayerHero.Age >= 10f
                && x.PlayerHero.Age < 21f
                && x.NextTrainingDecisionTime.IsPast);

            if (contract == null)
            {
                return;
            }

            List<InquiryElement> options = new()
            {
                new InquiryElement(
                    (int)HostageTrainingFocus.Cultural,
                    "随东道主文化学习",
                    null,
                    true,
                    "继续以东道主文化的传统技能为主。"),

                new InquiryElement(
                    (int)HostageTrainingFocus.Martial,
                    "侧重军旅",
                    null,
                    true,
                    "额外训练单手、运动与统御。"),

                new InquiryElement(
                    (int)HostageTrainingFocus.Stewardship,
                    "侧重政务",
                    null,
                    true,
                    "额外训练管理、贸易与工程。"),

                new InquiryElement(
                    (int)HostageTrainingFocus.Diplomacy,
                    "侧重外交",
                    null,
                    true,
                    "额外训练魅力、统御与贸易。")
            };

            string description =
                "收到"
                + contract.PlayerHero.Name
                + "从"
                + contract.ForeignOriginalClan.Name
                + "寄来的家书。你可以决定下一阶段的培养方向。";

            MultiSelectionInquiryData inquiry = new(
                "质子家书",
                description,
                options,
                false,
                1,
                1,
                "确定",
                string.Empty,
                selected => HandleTrainingFocusDecision(contract, selected),
                null,
                string.Empty,
                false);

            MBInformationManager.ShowMultiSelectionInquiry(
                inquiry,
                true,
                true);
        }

        private void HandleTrainingFocusDecision(
            HostageContract contract,
            List<InquiryElement> selected)
        {
            if (!_contracts.Contains(contract)
                || selected == null
                || selected.Count == 0)
            {
                return;
            }

            int focus = (int)selected[0].Identifier;
            contract.SetTrainingFocus(focus);

            MBInformationManager.AddQuickInformation(
                new TextObject("{=!}新的质子培养方向已经确定。"));
        }

        private void RenewContract(HostageContract contract)
        {
            if (!_contracts.Contains(contract)
                || !contract.CanRenew(1f))
            {
                MBInformationManager.AddQuickInformation(
                    new TextObject(
                        "{=!}这份质子约定已经不能继续延长。"));
                return;
            }

            contract.Renew(1f);
            ChangeForeignClanRelation(contract, 1, true);

            TextObject message = new TextObject(
                "{=!}{PLAYER_HERO.NAME}与{FOREIGN_HERO.NAME}的质子约定续约一年。");

            message.SetCharacterProperties(
                "PLAYER_HERO",
                contract.PlayerHero.CharacterObject,
                false);

            message.SetCharacterProperties(
                "FOREIGN_HERO",
                contract.ForeignHero.CharacterObject,
                false);

            MBInformationManager.AddQuickInformation(message);
        }

        private void MakePermanent(HostageContract contract)
        {
            if (!_contracts.Contains(contract))
            {
                return;
            }

            if (!HostageProgressModel.CanMakePermanent(
                contract,
                out string reason))
            {
                MBInformationManager.AddQuickInformation(
                    new TextObject("{=!}" + reason));
                return;
            }

            _contracts.Remove(contract);

            if (contract.PlayerHero.IsAlive
                && contract.PlayerHero.Age
                    >= Campaign.Current.Models.AgeModel.HeroComesOfAge)
            {
                HostageEducationPatch.RunOriginalCatchUp(
                    contract.PlayerHero);
            }

            ChangeForeignClanRelation(contract, 5, true);

            TextObject message = new TextObject(
                "{=!}永久质子交换生效：{PLAYER_HERO.NAME}与{FOREIGN_HERO.NAME}保留当前Clan归属。");

            message.SetCharacterProperties(
                "PLAYER_HERO",
                contract.PlayerHero.CharacterObject,
                false);

            message.SetCharacterProperties(
                "FOREIGN_HERO",
                contract.ForeignHero.CharacterObject,
                false);

            MBInformationManager.AddQuickInformation(message);
        }

        private void FinishContract(
            HostageContract contract,
            ContractEndReason reason,
            bool notify)
        {
            if (!_contracts.Remove(contract))
            {
                return;
            }

            switch (reason)
            {
                case ContractEndReason.NormalExpiry:
                    ChangeForeignClanRelation(contract, 2, true);
                    break;
                case ContractEndReason.War:
                    ChangeForeignClanRelation(contract, -5, true);
                    break;
                case ContractEndReason.SuccessionRecall:
                    ChangeForeignClanRelation(contract, -2, true);
                    break;
            }

            ReturnPlayerHero(contract);
            ReturnForeignHero(contract);

            if (!notify)
            {
                return;
            }

            TextObject message;
            switch (reason)
            {
                case ContractEndReason.EarlyRecall:
                    message = new TextObject(
                        "{=!}你提前终止了质子交换，双方幸存成员已经归还。");
                    break;
                case ContractEndReason.War:
                    message = new TextObject(
                        "{=!}战争爆发，质子约定自动终止，双方幸存成员被安全归还。");
                    break;
                case ContractEndReason.SuccessionRecall:
                    message = new TextObject(
                        "{=!}对方家族发生继承，新任Clan Leader召回了低整合度质子，约定结束。");
                    break;
                default:
                    message = new TextObject(
                        "{=!}质子交换期满，双方幸存成员已经归还原家族。");
                    break;
            }

            MBInformationManager.AddQuickInformation(message);
        }

        private void ReturnPlayerHero(HostageContract contract)
        {
            Hero hero = contract.PlayerHero;
            if (!hero.IsAlive || contract.PlayerOriginalClan.IsEliminated)
            {
                return;
            }

            bool isAdult =
                hero.Age
                >= Campaign.Current.Models.AgeModel.HeroComesOfAge;

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
            if (!hero.IsAlive
                || contract.ForeignOriginalClan.IsEliminated)
            {
                return;
            }

            HostageTransferService.TransferHero(
                hero,
                contract.ForeignOriginalClan,
                false);
        }

        private void ChangeForeignClanRelation(
            HostageContract contract,
            int relationChange,
            bool showNotification)
        {
            Hero? leader = contract.ForeignOriginalClan.Leader;
            if (leader == null || !leader.IsAlive || relationChange == 0)
            {
                return;
            }

            ChangeRelationAction.ApplyPlayerRelation(
                leader,
                relationChange,
                false,
                showNotification);
        }

        private void ClearExchangeSelection()
        {
            _selectedPlayerHero = null;
            _selectedForeignHero = null;
        }

        private enum ContractEndReason
        {
            NormalExpiry,
            EarlyRecall,
            War,
            SuccessionRecall
        }
    }
}
