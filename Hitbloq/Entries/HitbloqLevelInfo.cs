using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hitbloq.Entries
{
    internal class HitbloqLevelInfo
    {
        [JsonProperty("star_rating")]
        public ReadOnlyDictionary<string, float> Pools { get; private set; } = null!;
    }
}
