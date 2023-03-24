using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class HitbloqActionQueueEntry
	{
		[JsonProperty("_id")]
		public string ID { get; private set; } = null!;
	}
}