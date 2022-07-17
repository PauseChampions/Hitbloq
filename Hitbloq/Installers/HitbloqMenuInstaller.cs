using Zenject;
using Hitbloq.UI;
using Hitbloq.Managers;
using Hitbloq.Sources;
using Hitbloq.Other;
using IPA.Loader;
using System;
using MorePlaylists.UI;

namespace Hitbloq.Installers
{
    internal class HitbloqMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<HitbloqLeaderboardViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqPanelController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqProfileModalController>().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqEventModalViewController>().AsSingle();

            Container.BindInterfacesTo<MenuButtonUI>().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqNavigationController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqPoolListViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqPoolDetailViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqRankedListViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqPoolLeaderboardViewController>().FromNewComponentAsViewController().AsSingle();

            Container.BindInterfacesTo<HitbloqCustomLeaderboard>().AsSingle();
            Container.BindInterfacesTo<HitbloqManager>().AsSingle();

            Container.Bind<UserIDSource>().AsSingle();
            Container.Bind<FriendIDSource>().AsSingle();
            Container.Bind<ProfileSource>().AsSingle();
            Container.Bind<RankInfoSource>().AsSingle();
            Container.Bind<LevelInfoSource>().AsSingle();
            Container.Bind<PoolInfoSource>().AsSingle();
            Container.Bind<PoolListSource>().AsSingle();
            Container.Bind<RankedListDetailedSource>().AsSingle();
            Container.BindInterfacesTo<GlobalLeaderboardSource>().AsSingle();
            Container.BindInterfacesTo<AroundMeLeaderboardSource>().AsSingle();
            Container.BindInterfacesAndSelfTo<FriendsLeaderboardSource>().AsSingle();
            Container.BindInterfacesTo<GlobalPoolLeaderboardSource>().AsSingle();
            Container.BindInterfacesTo<AroundMePoolLeaderboardSource>().AsSingle();
            Container.BindInterfacesTo<FriendsPoolLeaderboardSource>().AsSingle();
            
#if DEBUG
            Container.BindInterfacesTo<DebugEventSource>().AsSingle();
#else
            Container.BindInterfacesTo<EventSource>().AsSingle();
#endif
            Container.Bind<SpriteLoader>().AsSingle();
            Container.Bind<MaterialGrabber>().AsSingle();
            Container.Bind<LeaderboardRefresher>().AsSingle();

            var playlistManager = PluginManager.GetPluginFromId("PlaylistManager");
            if (playlistManager != null && playlistManager.Assembly.GetName().Version >= new Version("1.5.0"))
            {
                Container.BindInterfacesAndSelfTo<PlaylistManagerIHardlyKnowHer>().AsSingle();
            }

            Container.BindInterfacesTo<AutomaticRegistration>().AsSingle();
        }
    }
}
