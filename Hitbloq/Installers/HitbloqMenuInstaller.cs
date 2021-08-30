using Zenject;
using SiraUtil;
using Hitbloq.UI;

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
        }
    }
}
