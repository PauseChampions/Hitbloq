using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqRegistrationEntry
    {
        [JsonProperty("status")]
        public string status;
    }
}