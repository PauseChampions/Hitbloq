using Zenject;

namespace Hitbloq.Other
{
	internal class BeatmapReporter : IInitializable
	{
		private readonly BeatmapListener _beatmapListener;
		private readonly IDifficultyBeatmap _difficultyBeatmap;

		public BeatmapReporter(IDifficultyBeatmap difficultyBeatmap, BeatmapListener beatmapListener)
		{
			_difficultyBeatmap = difficultyBeatmap;
			_beatmapListener = beatmapListener;
		}

		public void Initialize()
		{
			_beatmapListener.LastPlayedDifficultyBeatmap = _difficultyBeatmap;
		}
	}
}