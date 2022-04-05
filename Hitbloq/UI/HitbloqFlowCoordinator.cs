using HMUI;
using BeatSaberMarkupLanguage;
using Zenject;

namespace Hitbloq.UI
{
    internal class HitbloqFlowCoordinator : FlowCoordinator
    {
        [Inject]
        private readonly FlowCoordinator mainFlowCoordinator = null!;
        
        private FlowCoordinator? parentFlowCoordinator;
        
        [Inject]
        private readonly ViewController hitbloqMainViewController = null!;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            SetTitle("Hitbloq");
            showBackButton = true;
            ProvideInitialViewControllers(hitbloqMainViewController);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            parentFlowCoordinator.DismissFlowCoordinator(this);
        }

        internal void Show()
        {
            parentFlowCoordinator = DeepestChildFlowCoordinator(mainFlowCoordinator);
            parentFlowCoordinator.PresentFlowCoordinator(this);
        }

        private FlowCoordinator DeepestChildFlowCoordinator(FlowCoordinator root)
        {
            var flow = root.childFlowCoordinator;
            if (flow == null) return root;
            if (flow.childFlowCoordinator == null || flow.childFlowCoordinator == flow)
            {
                return flow;
            }
            return DeepestChildFlowCoordinator(flow);
        }
    }
}
