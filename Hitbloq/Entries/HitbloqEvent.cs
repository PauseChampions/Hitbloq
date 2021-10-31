using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqEvent
    {
        [JsonProperty("id")]
        public int id;

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string title;

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public string image;

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string description;

        [JsonProperty("pool", NullValueHandling = NullValueHandling.Ignore)]
        public string pool;
    }
}
