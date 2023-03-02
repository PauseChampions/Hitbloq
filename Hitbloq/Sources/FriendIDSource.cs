using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    internal class FriendIDSource
    {
        private readonly IHttpService siraHttpService;
        private readonly IPlatformUserModel platformUserModel;

        private HashSet<int>? hitbloqPlatformFriendIds;

        public FriendIDSource(IHttpService siraHttpService, IPlatformUserModel platformUserModel)
        {
            this.siraHttpService = siraHttpService;
            this.platformUserModel = platformUserModel;
        }

        public async Task<List<int>> GetFriendIDsAsync(CancellationToken cancellationToken = default)
        {
            await GetPlatformFriendIDsAsync(cancellationToken);
            return hitbloqPlatformFriendIds != null ? hitbloqPlatformFriendIds.Union(PluginConfig.Instance.Friends).ToList() : PluginConfig.Instance.Friends;
        }

        public async Task<HashSet<int>?> GetPlatformFriendIDsAsync(CancellationToken cancellationToken = default)
        {
            if (hitbloqPlatformFriendIds == null)
            {
                var friendIDs = await platformUserModel.GetUserFriendsUserIds(true);
                if (friendIDs != null)
                {
                    try
                    {
                        // the list will be in a key called "ids" and will be a list string
                        // lol what
                        var content = new Dictionary<string, string[]>
                        {
                            { "ids", friendIDs.ToArray<string>()}
                        };
                        var webResponse = await siraHttpService.PostAsync($"{PluginConfig.Instance.HitbloqURL}/api/tools/mass_ss_to_hitbloq", content, cancellationToken).ConfigureAwait(false);
                        var hitbloqFriendIDs = await Utils.ParseWebResponse<List<HitbloqFriendID>>(webResponse);
                        
                        if (hitbloqFriendIDs != null)
                        {
                            hitbloqPlatformFriendIds = hitbloqFriendIDs.Select(x => x.ID).ToHashSet();
                        }
                    }
                    catch (TaskCanceledException) { }
                }
            }
            return hitbloqPlatformFriendIds;
        }
    }
}
