using System.Collections.ObjectModel;
using Hitbloq.Entries;
using Newtonsoft.Json;

namespace Hitbloq.Pages
{
    internal class SerializablePoolLeaderboardPage
    {
        [JsonProperty("ladder")] 
        public ReadOnlyCollection<HitbloqPoolLeaderboardEntry>? Ladder { get; private set; }
    }
}