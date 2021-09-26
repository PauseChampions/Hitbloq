using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Hitbloq.Entries
{
    internal class LeaderboardEntry
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

        [JsonProperty("user")]
        public int userID;

        [JsonProperty("rank")]
        public int rank;

        [JsonProperty("cr")]
        public Dictionary<string, float> cr;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            username = Regex.Replace(username, "<[^>]*(>|$)", "");
        }
    }
}
