using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Hitbloq.Entries
{
    internal class HitbloqPoolInfo
    {
        [JsonProperty("shown_name")]
        public string shownName;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            shownName = Regex.Replace(shownName, "<[^>]*(>|$)", "");
        }
    }
}
