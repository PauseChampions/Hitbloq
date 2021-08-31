using Newtonsoft.Json;
using System.Collections.Generic;

namespace Hitbloq.Entries
{
    internal class HitbloqLevelInfo
    {
        [JsonProperty("star_rating")]
        public Dictionary<string, float> pools;
    }
}
