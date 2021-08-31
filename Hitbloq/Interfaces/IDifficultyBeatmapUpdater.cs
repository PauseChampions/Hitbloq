using Hitbloq.Entries;

namespace Hitbloq.Interfaces
{
    internal interface IDifficultyBeatmapUpdater
    {
        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, LevelInfoEntry levelInfoEntry);
    }
}
