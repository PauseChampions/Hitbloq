using Zenject;
using SiraUtil;
using Hitbloq.UI;
using Hitbloq.Managers;
using Hitbloq.Sources;
using Hitbloq.Other;
using IPA.Loader;
using System;

namespace Hitbloq.Installers
{
    internal class HitbloqMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<HitbloqLeaderboardViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqPanelController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqMainViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqProfileModalController>().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqEventModalViewController>().AsSingle();

            Container.BindInterfacesAndSelfTo<HitbloqFlowCoordinator>().FromNewComponentOnRoot().AsSingle();

            Container.BindInterfacesTo<HitbloqCustomLeaderboard>().AsSingle();
            Container.BindInterfacesTo<HitbloqManager>().AsSingle();

            Container.Bind<UserIDSource>().AsSingle();
            Container.Bind<FriendIDSource>().AsSingle();
            Container.Bind<ProfileSource>().AsSingle();
            Container.Bind<RankInfoSource>().AsSingle();
            Container.Bind<LevelInfoSource>().AsSingle();
            Container.Bind<PoolInfoSource>().AsSingle();
            Container.Bind<EventSource>().AsSingle();
            Container.BindInterfacesTo<GlobalLeaderboardSource>().AsSingle();
            Container.BindInterfacesTo<AroundMeLeaderboardSource>().AsSingle();
            Container.BindInterfacesAndSelfTo<FriendsLeaderboardSource>().AsSingle();

            Container.Bind<SpriteLoader>().AsSingle();
            Container.Bind<LeaderboardRefresher>().AsSingle();

            PluginMetadata playlistManager = PluginManager.GetPluginFromId("PlaylistManager");
            if (playlistManager != null && playlistManager.Assembly.GetName().Version >= new Version("1.5.0"))
            {
                Container.Bind<PlaylistManagerIHardlyKnowHer>().AsSingle();
            }

            Container.BindInterfacesTo<AutomaticRegistration>().AsSingle();
        }
    }
}
