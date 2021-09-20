using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqRankInfo
    {
        [JsonProperty("rank")]
        public int rank;

        [JsonProperty("cr")]
        public float cr;

        [JsonProperty("ranked_score_count")]
        public int scoreCount;

        [JsonProperty("tier")]
        private string tier;

        [JsonProperty("username")]
        public string username;

        public string TierURL => $"https://hitbloq.com/static/ranks/{tier}.png";
    }
}
