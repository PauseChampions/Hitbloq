using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqUserID
    {
        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public int ID { get; private set; } = -1;

        [JsonProperty("registered")]
        public bool Registered { get; private set; }
    }
}
