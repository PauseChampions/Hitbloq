using LeaderboardCore.Interfaces;

namespace Hitbloq.Managers
{
	internal sealed class ScoreSaberUploadManager : INotifyScoreUpload
	{
		private readonly HitbloqManager _hitbloqManager;

		public ScoreSaberUploadManager(HitbloqManager hitbloqManager)
		{
			_hitbloqManager = hitbloqManager;
		}

		public void OnScoreUploaded()
		{
			_hitbloqManager.OnScoreUploaded();
		}
	}
}