using Hitbloq.UI;

namespace Hitbloq.Interfaces
{
	internal interface INotifyViewActivated
	{
		public void ViewActivated(HitbloqLeaderboardViewController leaderboardViewController, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling);
	}
}