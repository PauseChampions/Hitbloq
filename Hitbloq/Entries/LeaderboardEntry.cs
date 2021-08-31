using Newtonsoft.Json;
using System.Collections.Generic;

namespace Hitbloq.Entries
{
    internal struct LeaderboardEntry
    {
        [JsonProperty("accuracy")]
        public float accuracy;

        [JsonProperty("score")]
        public int score;

        [JsonProperty("song_id")]
        public string levelID;

        [JsonProperty("time_set")]
        public float timeSet;

        [JsonProperty("username")]
        public string username;

        [JsonProperty("rank")]
        public string rank;

        [JsonProperty("cr")]
        public Dictionary<string, float> cr;
    }
}
