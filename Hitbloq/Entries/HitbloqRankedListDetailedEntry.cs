using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class HitbloqRankedListDetailedEntry
	{
		[JsonProperty("song_name")]
		public string Name { get; private set; } = "";

		[JsonProperty("song_cover")]
		public string CoverURL { get; private set; } = "";

		[JsonProperty("song_difficulty")]
		public string Difficulty { get; private set; } = "";

		[JsonProperty("song_stars")]
		public float Stars { get; private set; }
	}
}