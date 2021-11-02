using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqEvent
    {
        [JsonProperty("id")]
        public int id = -1;

        [JsonProperty("urt_title", NullValueHandling = NullValueHandling.Ignore)]
        public string title;

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public string image;

        [JsonProperty("urt_description", NullValueHandling = NullValueHandling.Ignore)]
        public string description;

        [JsonProperty("pool", NullValueHandling = NullValueHandling.Ignore)]
        public string pool;
    }
}
