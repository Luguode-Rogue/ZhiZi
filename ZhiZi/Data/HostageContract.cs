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

        public Hero PlayerHero => _playerHero;
        public Hero ForeignHero => _foreignHero;
        public Clan PlayerOriginalClan => _playerOriginalClan;
        public Clan ForeignOriginalClan => _foreignOriginalClan;
        public CampaignTime StartTime => _startTime;
        public CampaignTime EndTime => _endTime;

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
        }

        public bool Contains(Hero hero)
        {
            return hero == _playerHero || hero == _foreignHero;
        }
    }
}
