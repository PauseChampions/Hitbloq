using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Pages;
using Hitbloq.Utilities;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class GlobalPoolLeaderboardSource : IPoolLeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private Sprite? icon;
        public string HoverHint => "Global";
        public Sprite Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.GlobalIcon.png");
                }
                return icon;
            }
        }
        
        public GlobalPoolLeaderboardSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
        }
        
        public async Task<PoolLeaderboardPage?> GetScoresAsync(string poolID, CancellationToken cancellationToken = default, int page = 0)
        {
            try
            {
                var webResponse = await siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/ladder/{poolID}/players/{page}", cancellationToken: cancellationToken).ConfigureAwait(false);
                var serializablePage = await Utils.ParseWebResponse<SerializablePoolLeaderboardPage>(webResponse);
                if (serializablePage is {Ladder:{}})
                {
                    return new PoolLeaderboardPage(this, serializablePage.Ladder, poolID, page);
                }
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}