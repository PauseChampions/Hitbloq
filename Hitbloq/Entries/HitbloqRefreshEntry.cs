using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqRefreshEntry
    {
        [JsonProperty("error")]
        public string? Error { get; private set; }

        [JsonProperty("id")]
        public string? ID { get; private set; }
    }
}
