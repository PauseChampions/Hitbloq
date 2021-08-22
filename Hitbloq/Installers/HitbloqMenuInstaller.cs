using Hitbloq.UI.ViewControllers;
using Zenject;
using SiraUtil;
using Hitbloq.UI;

namespace Hitbloq.Installers
{
    internal class HitbloqMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MainLeaderboardViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<HitbloqPanelController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesTo<HitbloqCustomLeaderboard>().AsSingle();
        }
    }
}
