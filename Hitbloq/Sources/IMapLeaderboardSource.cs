﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;

namespace Hitbloq.Sources
{
	internal interface IMapLeaderboardSource : ILeaderboardSource
	{
		public bool Scrollable { get; }
		public Task<List<HitbloqMapLeaderboardEntry>?> GetScoresAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken cancellationToken = default, int page = 0);
		public void ClearCache();
	}
}