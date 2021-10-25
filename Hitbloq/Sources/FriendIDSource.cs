using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Hitbloq.Configuration;

namespace Hitbloq.Sources
{
    internal class FriendIDSource
    {
        private readonly SiraClient siraClient;
        private readonly IPlatformUserModel platformUserModel;

        private HashSet<int> hitbloqPlatformFriendIds;

        public FriendIDSource(SiraClient siraClient, IPlatformUserModel platformUserModel)
        {
            this.siraClient = siraClient;
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
                        WebResponse webResponse = await siraClient.PostAsync($"https://hitbloq.com/api/tools/mass_ss_to_hitbloq", content, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

                        hitbloqPlatformFriendIds = Utils.ParseWebResponse<List<HitbloqFriendID>>(webResponse).Select(x => x.id).ToHashSet();
                    }
                    catch (TaskCanceledException) { }
                }
            }
            return hitbloqPlatformFriendIds;
        }
    }
}
