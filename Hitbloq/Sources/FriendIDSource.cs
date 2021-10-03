using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace Hitbloq.Sources
{
    internal class FriendIDSource : IInitializable
    {
        private readonly SiraClient siraClient;
        private readonly IPlatformUserModel platformUserModel;

        public FriendIDSource(SiraClient siraClient, IPlatformUserModel platformUserModel)
        {
            this.siraClient = siraClient;
            this.platformUserModel = platformUserModel;
        }

        public async Task<HitbloqLevelInfo> GetLevelInfoAsync(CancellationToken? cancellationToken = null)
        {
            IReadOnlyList<string> friendIDs = await platformUserModel.GetUserFriendsUserIds(true);
            if (friendIDs != null)
            {
                try
                {
                    // the list will be in a key called "ids" and will be a list string
                    WebResponse webResponse = await siraClient.PostAsync($"https://hitbloq.com/api/tools/mass_ss_to_hitbloq", null, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    Plugin.Log.Debug(webResponse.ContentToString());
                }
                catch (TaskCanceledException) { }
            }
            return null;
        }

        public async void Initialize()
        {
            await GetLevelInfoAsync();
        }
    }
}
