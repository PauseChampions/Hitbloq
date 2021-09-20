using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqUserID
    {
        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public int id = -1;

        [JsonProperty("registered")]
        public bool registered;
    }
}
