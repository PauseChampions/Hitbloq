using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System.Collections.Generic;
using UnityEngine;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqMainLeaderboardView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqMainLeaderboardView.bsml")]
    internal class HitbloqLeaderboardViewController : BSMLAutomaticViewController
    {
        [UIComponent("leaderboard")]
        private Transform leaderboardTransform;

        [UIComponent("leaderboard")]
        internal LeaderboardTableView leaderboard;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (firstActivation)
            {
                List<LeaderboardTableView.ScoreData> placeholder = new List<LeaderboardTableView.ScoreData>();
                for (int i = 0; i < 10; i++)
                {
                    placeholder.Add(new LeaderboardTableView.ScoreData(0, "", 0, false));
                }

                LeaderboardTableCell[] leaderboardTableCells = leaderboardTransform.GetComponentsInChildren<LeaderboardTableCell>(true);
                foreach (var leaderboardTableCell in leaderboardTableCells)
                {
                    leaderboardTableCell.transform.Find("PlayerName").GetComponent<CurvedTextMeshPro>().richText = true;
                }
            }

            List<LeaderboardTableView.ScoreData> scores = new List<LeaderboardTableView.ScoreData>();
            SetScores(scores, 1);
        }

        public void SetScores(List<LeaderboardTableView.ScoreData> scores, int myScorePos)
        {
            if (scores.Count == 0)
            {
                scores.Add(new LeaderboardTableView.ScoreData(0, "You haven't set a score on this leaderboard - <size=75%>(<color=#FFD42A>0%</color>)</size>", 0, false));
            }
            leaderboard.SetScores(scores, myScorePos);
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            leaderboardTransform.Find("LoadingControl").gameObject.SetActive(false);
        }
    }
}
