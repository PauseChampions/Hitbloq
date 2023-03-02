using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqPoolInfo
    {
        [JsonProperty("shown_name")]
        public string ShownName { get; private set; } = null!;

        [JsonProperty("_id")]
        public string ID { get; private set; } = null!;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            ShownName = Regex.Replace(ShownName, "<[^>]*(>|$)", "");
        }
    }
}
