using System.Collections.ObjectModel;
using Hitbloq.Entries;
using Newtonsoft.Json;

namespace Hitbloq.Pages
{
    public class SerializablePoolLeaderboardPage
    {
        [JsonProperty("ladder")] 
        public ReadOnlyCollection<HitbloqPoolLeaderboardEntry>? Ladder { get; private set; }
    }
}