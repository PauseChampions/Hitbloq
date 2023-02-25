using LeaderboardCore.Interfaces;

namespace Hitbloq.Managers
{
	internal sealed class ScoreSaberUploadManager : INotifyScoreUpload
	{
		private readonly HitbloqManager hitbloqManager;

		public ScoreSaberUploadManager(HitbloqManager hitbloqManager)
		{
			this.hitbloqManager = hitbloqManager;
		}

		public void OnScoreUploaded() => hitbloqManager.OnScoreUploaded();
	}
}