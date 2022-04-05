using Zenject;

namespace Hitbloq.Other
{
    internal class BeatmapReporter : IInitializable
    {
        private readonly IDifficultyBeatmap difficultyBeatmap;
        private readonly BeatmapListener beatmapListener;

        public BeatmapReporter(IDifficultyBeatmap difficultyBeatmap, BeatmapListener beatmapListener)
        {
            this.difficultyBeatmap = difficultyBeatmap;
            this.beatmapListener = beatmapListener;
        }

        public void Initialize()
        {
            beatmapListener.LastPlayedDifficultyBeatmap = difficultyBeatmap;
        }
    }
}
