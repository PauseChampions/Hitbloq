using Hitbloq.Entries;
using Hitbloq.Utilities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Hitbloq.Configuration;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    internal class FriendIDSource
    {
        private readonly IHttpService siraHttpService;
        private readonly IPlatformUserModel platformUserModel;

        private HashSet<int> hitbloqPlatformFriendIds;

        public FriendIDSource(IHttpService siraHttpService, IPlatformUserModel platformUserModel)
        {
            this.siraHttpService = siraHttpService;
            this.platformUserModel = platformUserModel;
        }

        public async Task<List<int>> GetFriendIDsAsync(CancellationToken? cancellationToken = null)
        {
            await GetPlatformFriendIDsAsync(cancellationToken);
            return hitbloqPlatformFriendIds.Union(PluginConfig.Instance.Friends).ToList();
        }


        public async Task<HashSet<int>> GetPlatformFriendIDsAsync(CancellationToken? cancellationToken = null)
        {
            if (hitbloqPlatformFriendIds == null)
            {
                IReadOnlyList<string> friendIDs = await platformUserModel.GetUserFriendsUserIds(true);
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
                        IHttpResponse webResponse = await siraHttpService.PostAsync($"https://hitbloq.com/api/tools/mass_ss_to_hitbloq", content, cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

                        hitbloqPlatformFriendIds = (await Utils.ParseWebResponse<List<HitbloqFriendID>>(webResponse)).Select(x => x.ID).ToHashSet();
                    }
                    catch (TaskCanceledException) { }
                }
            }
            return hitbloqPlatformFriendIds;
        }
    }
}
