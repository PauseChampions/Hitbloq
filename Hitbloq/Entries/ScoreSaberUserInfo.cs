using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class ScoreSaberUserInfo
    {
        [JsonProperty("errorMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string errorMessage;
    }
}
