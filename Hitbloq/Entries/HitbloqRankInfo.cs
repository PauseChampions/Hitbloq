using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqRankInfo
    {
        [JsonProperty("rank")]
        public int Rank { get; private set; }

        [JsonProperty("cr")]
        public float CR { get; private set; }

        [JsonProperty("ranked_score_count")]
        public int ScoreCount { get; private set; }

        [JsonProperty("tier")]
        private string Tier { get; set; } = null!;

        [JsonProperty("username")]
        public string Username { get; private set; } = null!;

        public string TierURL => $"https://hitbloq.com/static/ranks/{Tier}.png";

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Username = Regex.Replace(Username, "<[^>]*(>|$)", "");
        }
    }
}
