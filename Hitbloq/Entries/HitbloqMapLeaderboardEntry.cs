using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Hitbloq.Entries
{
	internal class HitbloqMapLeaderboardEntry
	{
		[JsonProperty("accuracy")]
		public float Accuracy { get; private set; }

		[JsonProperty("score")]
		public int Score { get; private set; }

		[JsonProperty("song_id")]
		public string LevelID { get; private set; } = null!;

		[JsonProperty("date_set")]
		public string DateSet { get; private set; } = null!;

		[JsonProperty("username")]
		public string Username { get; private set; } = null!;

		[JsonProperty("user")]
		public int UserID { get; private set; }

		[JsonProperty("rank")]
		public int Rank { get; private set; }

		[JsonProperty("custom_color")]
		public string? CustomColor { get; private set; }

		[JsonProperty("cr")]
		public ReadOnlyDictionary<string, float> CR { get; private set; } = null!;

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			Username = Regex.Replace(Username, "<[^>]*(>|$)", "");
		}
	}
}