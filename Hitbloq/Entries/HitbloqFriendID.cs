using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqFriendID
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int id = -1;
    }
}
