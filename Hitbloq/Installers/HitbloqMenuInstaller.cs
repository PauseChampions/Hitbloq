using Zenject;
using SiraUtil;
using Hitbloq.UI;
using Hitbloq.Managers;
using Hitbloq.Sources;

namespace Hitbloq.Installers
{
    internal class HitbloqMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<HitbloqLeaderboardViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqPanelController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqMainViewController>().FromNewComponentAsViewController().AsSingle();

            Container.BindInterfacesAndSelfTo<HitbloqFlowCoordinator>().FromNewComponentOnRoot().AsSingle();

            Container.BindInterfacesTo<HitbloqCustomLeaderboard>().AsSingle();
            Container.BindInterfacesTo<HitbloqManager>().AsSingle();

            Container.Bind<UserInfoSource>().AsSingle();
            Container.Bind<RankInfoSource>().AsSingle();
            Container.Bind<LevelInfoSource>().AsSingle();
            Container.Bind<PoolInfoSource>().AsSingle();
            Container.Bind<LeaderboardRefresher>().AsSingle();
            Container.BindInterfacesTo<GlobalLeaderboardSource>().AsSingle();
            Container.BindInterfacesTo<AroundMeLeaderboardSource>().AsSingle();
        }
    }
}
