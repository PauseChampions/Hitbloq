using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class HitbloqLevelInfo
	{
		[JsonProperty("star_rating")]
		public ReadOnlyDictionary<string, float> Pools { get; private set; } = null!;
	}
}