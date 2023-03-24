using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Loader;
using SiraUtil.Zenject;
using Zenject;

namespace Hitbloq.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqInfoView.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqInfoView.bsml")]
	internal class HitbloqInfoViewController : BSMLAutomaticViewController
	{
		[Inject]
		private readonly UBinder<Plugin, PluginMetadata> _pluginMetadata = null!;

		[UIValue("version")]
		private string Version => $"{_pluginMetadata.Value.Name} v{_pluginMetadata.Value.HVersion}";

		public event Action? TutorialOpenRequested;
		public event Action<string>? URLOpenRequested;

		[UIAction("tutorial-click")]
		private void TutorialClicked()
		{
			TutorialOpenRequested?.Invoke();
		}

		[UIAction("website-click")]
		private void WebsiteClicked()
		{
			URLOpenRequested?.Invoke("https://hitbloq.com/");
		}

		[UIAction("discord-click")]
		private void DiscordClicked()
		{
			URLOpenRequested?.Invoke("https://hitbloq.com/discord");
		}

		[UIAction("github-click")]
		private void GithubClicked()
		{
			URLOpenRequested?.Invoke("https://github.com/PauseChampions/Hitbloq");
		}
	}
}