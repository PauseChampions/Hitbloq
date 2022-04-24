using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Pages;
using Hitbloq.Utilities;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class AroundMePoolLeaderboardSource : IPoolLeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private readonly UserIDSource userIDSource;
        private Sprite? icon;
        
        public string HoverHint => "Around Me";
        public Sprite Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.PlayerIcon.png");
                }
                return icon;
            }
        }
        
        public AroundMePoolLeaderboardSource(IHttpService siraHttpService, UserIDSource userIDSource)
        {
            this.siraHttpService = siraHttpService;
            this.userIDSource = userIDSource;
        }
        
        public async Task<PoolLeaderboardPage?> GetScoresAsync(string poolID, CancellationToken cancellationToken = default, int page = 0)
        {
            var userID = await userIDSource.GetUserIDAsync(cancellationToken);
            if (userID == null || userID.ID == -1)
            {
                return null;
            }
            var id = userID.ID;
            
            try
            {
                var webResponse = await siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/ladder/{poolID}/nearby_players/{id}", cancellationToken: cancellationToken).ConfigureAwait(false);
                var serializablePage = await Utils.ParseWebResponse<SerializablePoolLeaderboardPage>(webResponse);
                if (serializablePage is {Ladder:{}})
                {
                    return new PoolLeaderboardPage(this, serializablePage.Ladder, poolID, page, true);
                }
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}