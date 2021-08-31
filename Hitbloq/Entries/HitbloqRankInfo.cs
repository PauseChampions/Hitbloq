using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqRankInfo
    {
        [JsonProperty("rank")]
        public int rank;

        [JsonProperty("cr")]
        public float cr;
    }
}
