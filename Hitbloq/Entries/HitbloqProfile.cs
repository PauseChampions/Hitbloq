using Newtonsoft.Json;

namespace Hitbloq.Entries
{
    internal class HitbloqProfile
    {
        [JsonProperty("profile_pic")]
        public string profilePictureURL;

        [JsonProperty("profile_background")]
        public string profileBackgroundURL;
    }
}
