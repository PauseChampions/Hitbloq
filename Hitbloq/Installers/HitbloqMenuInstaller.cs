using Zenject;
using SiraUtil;
using Hitbloq.UI;
using Hitbloq.Managers;
using Hitbloq.Sources;
using Hitbloq.Other;

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

            Container.BindInterfacesAndSelfTo<HitbloqFlowCoordinator>().FromNewComponentOnRoot().AsSingle();

            Container.BindInterfacesTo<HitbloqCustomLeaderboard>().AsSingle();
            Container.BindInterfacesTo<HitbloqManager>().AsSingle();

            Container.Bind<UserIDSource>().AsSingle();
            Container.Bind<ProfileSource>().AsSingle();
            Container.Bind<RankInfoSource>().AsSingle();
            Container.Bind<LevelInfoSource>().AsSingle();
            Container.Bind<PoolInfoSource>().AsSingle();
            Container.BindInterfacesTo<GlobalLeaderboardSource>().AsSingle();
            Container.BindInterfacesTo<AroundMeLeaderboardSource>().AsSingle();

            Container.Bind<SpriteLoader>().AsSingle();
            Container.Bind<LeaderboardRefresher>().AsSingle();
            Container.BindInterfacesTo<AutomaticRegistration>().AsSingle();
        }
    }
}
