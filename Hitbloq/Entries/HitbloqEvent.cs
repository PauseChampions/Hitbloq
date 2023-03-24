using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class HitbloqEvent
	{
		[JsonProperty("id")]
		public int ID { get; set; } = -1;

		[JsonProperty("urt_title")]
		public string? Title { get; set; }

		[JsonProperty("image")]
		public string? Image { get; set; }

		[JsonProperty("urt_description")]
		public string? Description { get; set; }

		[JsonProperty("pool")]
		public string? Pool { get; set; }
	}
}