using System;
using BeatLeader.API;
using BeatLeader.API.Methods;
using BeatLeader.Models;
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
			UploadReplayRequest.RemoveStateListener(OnUploadReplayRequestStateChange);
		}

		public void Initialize()
		{
			UploadReplayRequest.AddStateListener(OnUploadReplayRequestStateChange);
		}

		private void OnUploadReplayRequestStateChange(RequestState state, Score result, string failReason)
		{
			if (state is RequestState.Finished)
			{
				_hitbloqManager.OnScoreUploaded();
			}
		}
	}
}