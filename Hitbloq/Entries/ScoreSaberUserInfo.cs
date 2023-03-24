using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class ScoreSaberUserInfo
	{
		[JsonProperty("errorMessage")]
		public string? ErrorMessage { get; private set; }
	}
}