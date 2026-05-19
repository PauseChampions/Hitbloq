using System;
using BeatLeader.API;
using BeatLeader.Models;
using BeatLeader.WebRequests;
using Zenject;

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

		private void OnUploadReplayRequestStateChange(IWebRequest<ScoreUploadResponse> request, RequestState state, string? failReason)
		{
			if (state is RequestState.Finished)
			{
				_hitbloqManager.OnScoreUploaded();
			}
		}
	}
}
