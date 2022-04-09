using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using BeatSaberMarkupLanguage.Attributes;
using Hitbloq.Configuration;
using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    public class HitbloqPoolListEntry
    {
        [JsonProperty("title")] 
        public string Title { get; private set; } = "";
        
        [JsonProperty("banner_image")] 
        public string? BannerImageURL { get; private set; }

        [JsonProperty("banner_title_hide")]
        public bool BannerTitleHide { get; private set; }
        
        [JsonProperty("player_count")]
        public int PlayerCount { get; private set; }
        
        [JsonProperty("popularity")]
        public int Popularity { get; private set; }
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (BannerImageURL != null && BannerImageURL.StartsWith("/"))
            {
                BannerImageURL = PluginConfig.Instance.HitbloqURL + BannerImageURL;
            }
        }
    }
}