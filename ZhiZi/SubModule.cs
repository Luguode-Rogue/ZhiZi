using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using ZhiZi.CampaignBehaviors;

namespace ZhiZi
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony? _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _harmony = new Harmony("LuguodeRogue.ZhiZi");
            _harmony.PatchAll();
        }

        protected override void OnSubModuleUnloaded()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            base.OnSubModuleUnloaded();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (gameStarterObject is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddBehavior(new HostageExchangeBehavior());
            }
        }
    }
}
