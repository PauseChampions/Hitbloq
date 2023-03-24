using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class HitbloqProfile
	{
		[JsonProperty("profile_pic")]
		public string? ProfilePictureURL { get; private set; }

		[JsonProperty("profile_background")]
		public string? ProfileBackgroundURL { get; private set; }
	}
}