using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace ZhiZi.Data
{
    public sealed class HostageContract
    {
        [SaveableField(1)]
        private Hero _playerHero = null!;

        [SaveableField(2)]
        private Hero _foreignHero = null!;

        [SaveableField(3)]
        private Clan _playerOriginalClan = null!;

        [SaveableField(4)]
        private Clan _foreignOriginalClan = null!;

        [SaveableField(5)]
        private CampaignTime _startTime;

        [SaveableField(6)]
        private CampaignTime _endTime;

        [SaveableField(7)]
        private float _integrationProgress;

        [SaveableField(8)]
        private CampaignTime _lastIntegrationUpdate;

        [SaveableField(9)]
        private CampaignTime _lastTrainingUpdate;

        [SaveableField(10)]
        private int _renewalCount;

        [SaveableField(11)]
        private Hero? _foreignLeaderAtStart;

        [SaveableField(12)]
        private bool _successionResolved;

        [SaveableField(13)]
        private int _trainingFocus;

        [SaveableField(14)]
        private CampaignTime _nextTrainingDecisionTime;

        [SaveableField(15)]
        private bool _hasBattleExperience;

        [SaveableField(16)]
        private int _lowRelationStage;

        [SaveableField(17)]
        private int _highRelationStage;

        [SaveableField(18)]
        private bool _wantsToStay;

        [SaveableField(19)]
        private bool _devotedToPlayerClan;

        [SaveableField(20)]
        private CampaignTime _lastBattleRelationTime;

        [SaveableField(21)]
        private CampaignTime _lastWoundedTime;

        [SaveableField(22)]
        private CampaignTime _captivityStartTime;

        [SaveableField(23)]
        private int _captivityPenaltyStage;

        [SaveableField(24)]
        private CampaignTime _lastMeaningfulServiceTime;

        [SaveableField(25)]
        private CampaignTime _lastIdlePenaltyTime;

        [SaveableField(26)]
        private bool _wasGovernor;

        [SaveableField(27)]
        private CampaignTime _governorSince;

        [SaveableField(28)]
        private bool _governorAppointmentRewarded;

        [SaveableField(29)]
        private bool _wasPartyLeader;

        [SaveableField(30)]
        private CampaignTime _partyLeaderSince;

        [SaveableField(31)]
        private bool _partyLeadershipRewarded;

        [SaveableField(32)]
        private CampaignTime _lastLongServiceRewardTime;

        [SaveableField(33)]
        private int _annualRelationRewardCount;

        [SaveableField(34)]
        private bool _hasFoughtAlongsidePlayer;

        public Hero PlayerHero => _playerHero;
        public Hero ForeignHero => _foreignHero;
        public Clan PlayerOriginalClan => _playerOriginalClan;
        public Clan ForeignOriginalClan => _foreignOriginalClan;
        public CampaignTime StartTime => _startTime;
        public CampaignTime EndTime => _endTime;
        public float IntegrationProgress => _integrationProgress;
        public CampaignTime LastIntegrationUpdate => _lastIntegrationUpdate;
        public CampaignTime LastTrainingUpdate => _lastTrainingUpdate;
        public int RenewalCount => _renewalCount;
        public Hero? ForeignLeaderAtStart => _foreignLeaderAtStart;
        public bool SuccessionResolved => _successionResolved;
        public int TrainingFocus => _trainingFocus;
        public CampaignTime NextTrainingDecisionTime => _nextTrainingDecisionTime;
        public bool HasBattleExperience => _hasBattleExperience;
        public int LowRelationStage => _lowRelationStage;
        public int HighRelationStage => _highRelationStage;
        public bool WantsToStay => _wantsToStay;
        public bool DevotedToPlayerClan => _devotedToPlayerClan;
        public CampaignTime LastBattleRelationTime => _lastBattleRelationTime;
        public CampaignTime LastWoundedTime => _lastWoundedTime;
        public CampaignTime CaptivityStartTime => _captivityStartTime;
        public int CaptivityPenaltyStage => _captivityPenaltyStage;
        public CampaignTime LastMeaningfulServiceTime => _lastMeaningfulServiceTime;
        public CampaignTime LastIdlePenaltyTime => _lastIdlePenaltyTime;
        public bool WasGovernor => _wasGovernor;
        public CampaignTime GovernorSince => _governorSince;
        public bool GovernorAppointmentRewarded => _governorAppointmentRewarded;
        public bool WasPartyLeader => _wasPartyLeader;
        public CampaignTime PartyLeaderSince => _partyLeaderSince;
        public bool PartyLeadershipRewarded => _partyLeadershipRewarded;
        public CampaignTime LastLongServiceRewardTime => _lastLongServiceRewardTime;
        public int AnnualRelationRewardCount => _annualRelationRewardCount;
        public bool HasFoughtAlongsidePlayer => _hasFoughtAlongsidePlayer;

        public HostageContract()
        {
        }

        public HostageContract(
            Hero playerHero,
            Hero foreignHero,
            Clan playerOriginalClan,
            Clan foreignOriginalClan,
            CampaignTime startTime,
            CampaignTime endTime)
        {
            _playerHero = playerHero;
            _foreignHero = foreignHero;
            _playerOriginalClan = playerOriginalClan;
            _foreignOriginalClan = foreignOriginalClan;
            _startTime = startTime;
            _endTime = endTime;
            _integrationProgress = 0f;
            _lastIntegrationUpdate = startTime;
            _lastTrainingUpdate = startTime;
            _renewalCount = 0;
            _foreignLeaderAtStart = foreignOriginalClan.Leader;
            _successionResolved = false;
            _trainingFocus = 0;
            _nextTrainingDecisionTime = startTime + CampaignTime.Years(1f);
            _hasBattleExperience = false;
            _lowRelationStage = 0;
            _highRelationStage = 0;
            _wantsToStay = false;
            _devotedToPlayerClan = false;
            _lastBattleRelationTime = CampaignTime.Zero;
            _lastWoundedTime = CampaignTime.Zero;
            _captivityStartTime = CampaignTime.Zero;
            _captivityPenaltyStage = 0;
            _lastMeaningfulServiceTime = startTime;
            _lastIdlePenaltyTime = CampaignTime.Zero;
            _wasGovernor = false;
            _governorSince = CampaignTime.Zero;
            _governorAppointmentRewarded = false;
            _wasPartyLeader = false;
            _partyLeaderSince = CampaignTime.Zero;
            _partyLeadershipRewarded = false;
            _lastLongServiceRewardTime = startTime;
            _annualRelationRewardCount = 0;
            _hasFoughtAlongsidePlayer = false;
        }

        public bool Contains(Hero hero)
        {
            return hero == _playerHero || hero == _foreignHero;
        }

        public void EnsureInitialized()
        {
            if (_lastIntegrationUpdate.ToDays <= 0d)
            {
                _lastIntegrationUpdate = _startTime;
            }

            if (_lastTrainingUpdate.ToDays <= 0d)
            {
                _lastTrainingUpdate = _startTime;
            }

            if (_nextTrainingDecisionTime.ToDays <= 0d)
            {
                _nextTrainingDecisionTime = _startTime + CampaignTime.Years(1f);
            }

            if (_foreignLeaderAtStart == null && _foreignOriginalClan != null)
            {
                _foreignLeaderAtStart = _foreignOriginalClan.Leader;
            }

            if (_lastMeaningfulServiceTime.ToDays <= 0d)
            {
                _lastMeaningfulServiceTime = _startTime;
            }

            if (_lastLongServiceRewardTime.ToDays <= 0d)
            {
                _lastLongServiceRewardTime = _startTime;
            }

            _integrationProgress = Math.Max(0f, Math.Min(100f, _integrationProgress));
            _lowRelationStage = Math.Max(0, Math.Min(3, _lowRelationStage));
            _highRelationStage = Math.Max(0, Math.Min(4, _highRelationStage));
            _captivityPenaltyStage = Math.Max(0, Math.Min(2, _captivityPenaltyStage));
        }

        public void AddIntegration(float amount)
        {
            _integrationProgress = Math.Max(
                0f,
                Math.Min(100f, _integrationProgress + amount));
        }

        public void MarkIntegrationUpdated()
        {
            _lastIntegrationUpdate = CampaignTime.Now;
        }

        public void MarkTrainingUpdated()
        {
            _lastTrainingUpdate = CampaignTime.Now;
        }

        public bool CanRenew(float years)
        {
            CampaignTime projectedEnd = _endTime + CampaignTime.Years(years);
            double projectedDurationDays = (projectedEnd - _startTime).ToDays;
            return projectedDurationDays <= CampaignTime.Years(10f).ToDays;
        }

        public void Renew(float years)
        {
            if (!CanRenew(years))
            {
                return;
            }

            _endTime += CampaignTime.Years(years);
            _renewalCount++;
        }

        public void ResolveSuccession()
        {
            _successionResolved = true;
        }

        public void SetTrainingFocus(int focus)
        {
            _trainingFocus = Math.Max(0, Math.Min(3, focus));
            _nextTrainingDecisionTime = CampaignTime.Now + CampaignTime.Years(1f);
        }

        public void MarkBattleExperience()
        {
            _hasBattleExperience = true;
        }

        public void SetLowRelationStage(int stage)
        {
            _lowRelationStage = Math.Max(_lowRelationStage, Math.Min(3, stage));
        }

        public void SetHighRelationStage(int stage)
        {
            _highRelationStage = Math.Max(_highRelationStage, Math.Min(4, stage));
        }

        public void MarkWantsToStay(bool devoted)
        {
            _wantsToStay = true;

            if (devoted)
            {
                _devotedToPlayerClan = true;
            }
        }

        public bool CanGrantBattleRelationReward()
        {
            return _lastBattleRelationTime.ToDays <= 0d
                || _lastBattleRelationTime.ElapsedDaysUntilNow >= 30f;
        }

        public void MarkBattleRelationReward()
        {
            _lastBattleRelationTime = CampaignTime.Now;
        }

        public bool WasWoundedRecently(float days)
        {
            return _lastWoundedTime.ToDays > 0d
                && _lastWoundedTime.ElapsedDaysUntilNow < days;
        }

        public void MarkWounded()
        {
            _lastWoundedTime = CampaignTime.Now;
        }

        public void MarkCaptured()
        {
            _captivityStartTime = CampaignTime.Now;
            _captivityPenaltyStage = 0;
        }

        public void EnsureCaptivityStarted()
        {
            if (_captivityStartTime.ToDays <= 0d)
            {
                _captivityStartTime = CampaignTime.Now;
            }
        }

        public void SetCaptivityPenaltyStage(int stage)
        {
            _captivityPenaltyStage = Math.Max(
                _captivityPenaltyStage,
                Math.Min(2, stage));
        }

        public void ClearCaptivity()
        {
            _captivityStartTime = CampaignTime.Zero;
            _captivityPenaltyStage = 0;
        }

        public void MarkMeaningfulService()
        {
            _lastMeaningfulServiceTime = CampaignTime.Now;
        }

        public bool CanApplyIdlePenalty()
        {
            if (_lastMeaningfulServiceTime.ElapsedDaysUntilNow < 180f)
            {
                return false;
            }

            return _lastIdlePenaltyTime.ToDays <= 0d
                || _lastIdlePenaltyTime.ElapsedDaysUntilNow >= 180f;
        }

        public void MarkIdlePenalty()
        {
            _lastIdlePenaltyTime = CampaignTime.Now;
        }

        public void BeginGovernorService()
        {
            _wasGovernor = true;
            _governorSince = CampaignTime.Now;
            MarkMeaningfulService();
        }

        public float EndGovernorService()
        {
            float duration = _governorSince.ToDays <= 0d
                ? float.MaxValue
                : _governorSince.ElapsedDaysUntilNow;

            _wasGovernor = false;
            _governorSince = CampaignTime.Zero;
            return duration;
        }

        public void RewardGovernorAppointment()
        {
            _governorAppointmentRewarded = true;
        }

        public void BeginPartyLeadership()
        {
            _wasPartyLeader = true;
            _partyLeaderSince = CampaignTime.Now;
            MarkMeaningfulService();
        }

        public float EndPartyLeadership()
        {
            float duration = _partyLeaderSince.ToDays <= 0d
                ? float.MaxValue
                : _partyLeaderSince.ElapsedDaysUntilNow;

            _wasPartyLeader = false;
            _partyLeaderSince = CampaignTime.Zero;
            return duration;
        }

        public void RewardPartyLeadership()
        {
            _partyLeadershipRewarded = true;
        }

        public bool CanRewardLongService()
        {
            return _lastLongServiceRewardTime.ElapsedDaysUntilNow >= 90f;
        }

        public void MarkLongServiceReward()
        {
            _lastLongServiceRewardTime = CampaignTime.Now;
            MarkMeaningfulService();
        }

        public void SetAnnualRelationRewardCount(int count)
        {
            _annualRelationRewardCount = Math.Max(_annualRelationRewardCount, count);
        }

        public void MarkFoughtAlongsidePlayer()
        {
            _hasFoughtAlongsidePlayer = true;
        }
    }
}
