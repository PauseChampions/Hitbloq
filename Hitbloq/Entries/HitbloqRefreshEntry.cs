using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqRefreshEntry
    {
        [JsonProperty("error")]
        public string error;

        [JsonProperty("id")]
        public string id;
    }
}
