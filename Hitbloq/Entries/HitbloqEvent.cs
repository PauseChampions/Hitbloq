using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqEvent
    {
        [JsonProperty("id")] 
        public int ID { get; private set; } = -1;

        [JsonProperty("urt_title")]
        public string? Title { get; private set; }

        [JsonProperty("image")]
        public string? Image { get; private set; }

        [JsonProperty("urt_description")]
        public string? Description { get; private set; }

        [JsonProperty("pool")]
        public string? Pool { get; private set; }
    }
}
