using Zenject;

namespace Hitbloq.Other
{
	internal class BeatmapReporter : IInitializable
	{
		private readonly BeatmapListener _beatmapListener;
		private readonly BeatmapKey _beatmapKey;

		public BeatmapReporter(BeatmapKey beatmapKey, BeatmapListener beatmapListener)
		{
			_beatmapKey = beatmapKey;
			_beatmapListener = beatmapListener;
		}

		public void Initialize()
		{
			_beatmapListener.LastPlayedBeatmapKey = _beatmapKey;
		}
	}
}