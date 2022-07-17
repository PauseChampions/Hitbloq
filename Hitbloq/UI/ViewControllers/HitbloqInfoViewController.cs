using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Loader;
using SiraUtil.Zenject;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqInfoView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqInfoView.bsml")]
    internal class HitbloqInfoViewController : BSMLAutomaticViewController
    {
        [Inject]
        private readonly UBinder<Plugin, PluginMetadata> pluginMetadata = null!;

        public event Action? TutorialOpenRequested;
        public event Action<string>? URLOpenRequested;
        
        [UIAction("tutorial-click")]
        private void TutorialClicked() => TutorialOpenRequested?.Invoke();
        
        [UIAction("website-click")]
        private void WebsiteClicked() => URLOpenRequested?.Invoke("https://hitbloq.com/");
        
        [UIAction("discord-click")]
        private void DiscordClicked() => URLOpenRequested?.Invoke("https://hitbloq.com/discord");
        
        [UIAction("github-click")]
        private void GithubClicked() => URLOpenRequested?.Invoke("https://github.com/PauseChampions/Hitbloq");
        
        [UIValue("version")]
        private string Version => $"{pluginMetadata.Value.Name} v{pluginMetadata.Value.HVersion}";
    }
}