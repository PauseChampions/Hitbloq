#if !HITBLOQ_BS_1_29_1
using System;
using BeatLeader.API;
using BeatLeader.Models;
using BeatLeader.WebRequests;
using Zenject;
#if HITBLOQ_BS_1_40_8
using BeatLeaderUploadResponse = BeatLeader.Models.Score;
#else
using BeatLeaderUploadResponse = BeatLeader.Models.ScoreUploadResponse;
#endif

namespace Hitbloq.Managers
{
	internal sealed class BeatLeaderUploadManager : IInitializable, IDisposable
	{
		private readonly HitbloqManager _hitbloqManager;

		public BeatLeaderUploadManager(HitbloqManager hitbloqManager)
		{
			_hitbloqManager = hitbloqManager;
		}

		public void Dispose()
		{
			UploadReplayRequest.StateChangedEvent -= OnUploadReplayRequestStateChange;
		}

		public void Initialize()
		{
			UploadReplayRequest.StateChangedEvent += OnUploadReplayRequestStateChange;
		}

		private void OnUploadReplayRequestStateChange(IWebRequest<BeatLeaderUploadResponse> request, RequestState state, string? failReason)
		{
			// Edited by GPT-5 Codex 2026-05-27
			// BeatLeader can finish replay work for plays that did not become an uploaded score.
			// Only completed uploads should ask Hitbloq to refresh leaderboard data.
			if (state is RequestState.Finished && IsCompletedScoreUpload(request))
			{
				_hitbloqManager.OnScoreUploaded();
			}
		}

		private static bool IsCompletedScoreUpload(IWebRequest<BeatLeaderUploadResponse> request)
		{
#if HITBLOQ_BS_1_40_8
			// BeatLeader 1.40.8 exposes the uploaded score directly, so a missing score is ignored.
			return request.Result != null;
#else
			// Newer BeatLeader responses expose upload status. Attempts and errors do not change scores.
			return request.Result?.Status is ScoreUploadStatus.Uploaded;
#endif
		}
	}
}
#endif
