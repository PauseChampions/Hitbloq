using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Hitbloq.Configuration;
using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqPoolListEntry
    {
        [JsonProperty("title")] 
        public string Title { get; private set; } = "";

        [JsonProperty("author")] 
        public string Author { get; private set; } = "Hitbloq";
        
        [JsonProperty("description")]
        public string? Description { get; private set; }
        
        [JsonProperty("short_description")]
        public string? ShortDescription { get; private set; }
        
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

            Description = Description?.Replace("&emsp;", " ");
            Description = Description?.Replace("&thinsp;", " ");
        }

        #region Comparers

        private static NameComparer? nameComparer;
        public static NameComparer NameComparer => nameComparer ??= new NameComparer();
        
        private static PlayerCountComparer? playerCountComparer;
        public static PlayerCountComparer PlayerCountComparer => playerCountComparer ??= new PlayerCountComparer();
        
        private static PopularityComparer? popularityComparer;
        public static PopularityComparer PopularityComparer => popularityComparer ??= new PopularityComparer();

        #endregion
    }
    
    internal class NameComparer : IComparer<HitbloqPoolListEntry>
    {
        public int Compare(HitbloqPoolListEntry? x, HitbloqPoolListEntry? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return string.Compare(x.Title, y.Title, StringComparison.Ordinal);
        }
    }
        
    internal class PlayerCountComparer : IComparer<HitbloqPoolListEntry>
    {
        public int Compare(HitbloqPoolListEntry? x, HitbloqPoolListEntry? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.PlayerCount.CompareTo(y.PlayerCount);
        }
    }
        
    internal class PopularityComparer : IComparer<HitbloqPoolListEntry>
    {
        public int Compare(HitbloqPoolListEntry? x, HitbloqPoolListEntry? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.Popularity.CompareTo(y.Popularity);
        }
    }
}