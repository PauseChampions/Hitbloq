using Hitbloq.Entries;

namespace Hitbloq.Interfaces
{
	internal interface IBeatmapKeyUpdater
	{
		public void BeatmapKeyUpdated(BeatmapKey beatmapKey, HitbloqLevelInfo? levelInfoEntry);
	}
}