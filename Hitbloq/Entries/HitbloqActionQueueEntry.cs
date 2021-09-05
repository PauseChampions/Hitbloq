using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqActionQueueEntry
    {
        [JsonProperty("_id")]
        public string id;
    }
}
