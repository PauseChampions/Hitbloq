using UnityEngine;

namespace Hitbloq.Sources
{
	public interface ILeaderboardSource
	{
		public string HoverHint { get; }
		public Sprite Icon { get; }
	}
}