using Newtonsoft.Json;
using System.Collections.Generic;

namespace Hitbloq.Entries
{
    internal class ScoreSaberUserInfo
    {
        [JsonProperty("playerInfo", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> playerInfo;
    }
}
