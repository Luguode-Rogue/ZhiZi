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

            _integrationProgress = Math.Max(0f, Math.Min(100f, _integrationProgress));
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
    }
}
