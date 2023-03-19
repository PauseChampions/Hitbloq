using Hitbloq.Configuration;
using Hitbloq.Installers;
using Hitbloq.Other;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace Hitbloq
{
	[Plugin(RuntimeOptions.DynamicInit)] [NoEnableDisable]
	public class Plugin
	{
        /// <summary>
        ///     Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        ///     [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        ///     Only use [Init] with one Constructor.
        /// </summary>
        [Init]
		public Plugin(IPALogger logger, Config conf, Zenjector zenjector)
		{
			Instance = this;
			Log = logger;
			PluginConfig.Instance = conf.Generated<PluginConfig>();
			zenjector.UseMetadataBinder<Plugin>();
			zenjector.UseHttpService();
			zenjector.Install(Location.App, container => { container.Bind<BeatmapListener>().AsSingle(); });
			zenjector.Install<HitbloqMenuInstaller>(Location.Menu);
			zenjector.Install(Location.Player, container => { container.BindInterfacesTo<BeatmapReporter>().AsSingle(); });
		}

		internal static Plugin Instance { get; private set; } = null!;
		internal static IPALogger Log { get; private set; } = null!;
	}
}