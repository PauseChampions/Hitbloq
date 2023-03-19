using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class HitbloqFriendID
	{
		[JsonProperty("id")]
		public int ID { get; private set; }
	}
}