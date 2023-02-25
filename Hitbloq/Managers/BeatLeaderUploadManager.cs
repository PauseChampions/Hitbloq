using System;
using BeatLeader.API;
using BeatLeader.API.Methods;
using Zenject;

namespace Hitbloq.Managers
{
	internal sealed class BeatLeaderUploadManager : IInitializable, IDisposable
	{
		private readonly HitbloqManager hitbloqManager;

		public BeatLeaderUploadManager(HitbloqManager hitbloqManager)
		{
			this.hitbloqManager = hitbloqManager;
		}

		public void Initialize()
		{
			UploadReplayRequest.AddStateListener(OnUploadReplayRequestStateChange);
		}

		public void Dispose()
		{
			UploadReplayRequest.RemoveStateListener(OnUploadReplayRequestStateChange);
		}

		private void OnUploadReplayRequestStateChange(RequestState state, BeatLeader.Models.Score result, string failReason)
		{
			if (state is RequestState.Finished)
				hitbloqManager.OnScoreUploaded();
		}
	}
}