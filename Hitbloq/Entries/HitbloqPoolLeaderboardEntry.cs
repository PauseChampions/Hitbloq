using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    public class HitbloqPoolLeaderboardEntry
    {
        [JsonProperty("username")]
        public string Username { get; private set; } = null!;

        [JsonProperty("user")]
        public int UserID { get; private set; }

        [JsonProperty("rank")]
        public int Rank { get; private set; }
        
        [JsonProperty("rank_change")]
        public int RankChange { get; private set; }
        
        [JsonProperty("custom_color")]
        public string? CustomColor { get; private set; }
        
        [JsonProperty("banner_image")]
        public string? BannerImageURL { get; private set; }
        
        [JsonProperty("profile_pic")]
        public string? ProfilePictureURL { get; private set; }

        [JsonProperty("cr")]
        public float CR { get; private set; }
    }
}