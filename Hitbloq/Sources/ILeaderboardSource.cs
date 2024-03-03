using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
	public interface ILeaderboardSource
	{
		public string HoverHint { get; }
		public Task<Sprite> Icon { get; }
	}
}