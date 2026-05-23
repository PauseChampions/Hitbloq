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
			if (state is RequestState.Finished)
			{
				_hitbloqManager.OnScoreUploaded();
			}
		}
	}
}
#endif
