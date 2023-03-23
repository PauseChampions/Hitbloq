using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities.Async;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Hitbloq.UI
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqLeaderboardView.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqLeaderboardView.bsml")]
	internal class HitbloqLeaderboardViewController : BSMLAutomaticViewController, IDifficultyBeatmapUpdater, ILeaderboardEntriesUpdater, IPoolUpdater
	{
		[UIComponent("leaderboard")]
		private readonly LeaderboardTableView? _leaderboard = null!;

		[Inject]
		private readonly List<IMapLeaderboardSource> _leaderboardSources = null!;

		[UIComponent("leaderboard")]
		private readonly Transform? _leaderboardTransform = null!;

		[Inject]
		private readonly HitbloqProfileModalController _profileModalController = null!;

		[Inject]
		private readonly UserIDSource _userIDSource = null!;

		private IDifficultyBeatmap? _difficultyBeatmap;

		private List<Button>? _infoButtons;
		private List<HitbloqMapLeaderboardEntry>? _leaderboardEntries;

		private LoadingControl? _loadingControl;

		private int _pageNumber;
		private string? _selectedPool;

		private int PageNumber
		{
			get => _pageNumber;
			set
			{
				_pageNumber = value;
				NotifyPropertyChanged(nameof(UpEnabled));
				if (_leaderboard != null && _loadingControl != null && _difficultyBeatmap != null && Utils.IsDependencyLeaderboardInstalled)
				{
					_leaderboard.SetScores(new List<LeaderboardTableView.ScoreData>(), 0);
					_loadingControl.gameObject.SetActive(true);
					PageRequested?.Invoke(_difficultyBeatmap, _leaderboardSources[SelectedCellIndex], value);
				}
			}
		}

		[UIValue("up-enabled")]
		private bool UpEnabled => PageNumber != 0 && _leaderboardSources[SelectedCellIndex].Scrollable;

		[UIValue("down-enabled")]
		private bool DownEnabled => _leaderboardEntries is {Count: 10} && _leaderboardSources[SelectedCellIndex].Scrollable;

		public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo? levelInfoEntry)
		{
			if (levelInfoEntry != null)
			{
				_difficultyBeatmap = difficultyBeatmap;
				if (isActiveAndEnabled)
				{
					foreach (var leaderboardSource in _leaderboardSources)
					{
						leaderboardSource.ClearCache();
					}

					PageNumber = 0;
				}
			}
		}

		public void LeaderboardEntriesUpdated(List<HitbloqMapLeaderboardEntry>? leaderboardEntries)
		{
			_leaderboardEntries = leaderboardEntries;
			NotifyPropertyChanged(nameof(DownEnabled));
			_ = SetScores(leaderboardEntries);
		}

		public void PoolUpdated(string pool)
		{
			_selectedPool = pool;
			if (isActiveAndEnabled)
			{
				_ = SetScores(_leaderboardEntries);
			}
		}

		public event Action<IDifficultyBeatmap, IMapLeaderboardSource, int>? PageRequested;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
			foreach (var leaderboardSource in _leaderboardSources)
			{
				leaderboardSource.ClearCache();
			}

			if (!firstActivation && !Utils.IsDependencyLeaderboardInstalled)
			{
				SetNoDependenciesInstalledText();
			}

			PageNumber = 0;
		}

		private async Task SetScores(List<HitbloqMapLeaderboardEntry>? leaderboardEntries)
		{
			if (Utils.IsDependencyLeaderboardInstalled is false)
				return;

			var scores = new List<LeaderboardTableView.ScoreData>();
			var myScorePos = -1;

			if (_infoButtons != null)
			{
				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					foreach (var button in _infoButtons)
					{
						button.gameObject.SetActive(false);
					}
				});
			}

			if (leaderboardEntries == null || leaderboardEntries.Count == 0)
			{
				scores.Add(new LeaderboardTableView.ScoreData(0, "You haven't set a score on this leaderboard - <size=75%>(<color=#FFD42A>0%</color>)</size>", 0, false));
			}
			else
			{
				if (_selectedPool == null || !leaderboardEntries.First().CR.ContainsKey(_selectedPool))
				{
					return;
				}

				var userID = await _userIDSource.GetUserIDAsync();
				var id = userID?.ID ?? -1;

				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					for (var i = 0; i < (leaderboardEntries.Count > 10 ? 10 : leaderboardEntries.Count); i++)
					{
						scores.Add(new LeaderboardTableView.ScoreData(leaderboardEntries[i].Score, $"<color={leaderboardEntries[i].CustomColor ?? "#ffffff"}><size=85%>{leaderboardEntries[i].Username}</color> - <size=75%>(<color=#FFD42A>{leaderboardEntries[i].Accuracy.ToString("F2")}%</color>)</size></size> - <size=75%> (<color=#aa6eff>{leaderboardEntries[i].CR[_selectedPool].ToString("F2")}<size=55%>cr</size></color>)</size>", leaderboardEntries[i].Rank, false));

						if (_infoButtons != null)
						{
							_infoButtons[i].gameObject.SetActive(true);
							var hoverHint = _infoButtons[i].GetComponent<HoverHint>();
							hoverHint.text = $"Score Set: {leaderboardEntries[i].DateSet}";
						}

						if (leaderboardEntries[i].UserID == id)
						{
							myScorePos = i;
						}
					}
				});
			}

			if (_loadingControl != null && _leaderboard != null)
			{
				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					_loadingControl.gameObject.SetActive(false);
					_leaderboard.SetScores(scores, myScorePos);
				});
			}
		}

		private void ChangeButtonScale(Button button, float scale)
		{
			var transform = button.transform;
			var localScale = transform.localScale;
			transform.localScale = localScale * scale;
			_infoButtons?.Add(button);
		}

		public void InfoButtonClicked(int index)
		{
			if (_leaderboardEntries != null && index < _leaderboardEntries.Count && _selectedPool != null)
			{
				_profileModalController.ShowModalForUser(transform, _leaderboardEntries[index].UserID, _selectedPool);
			}
		}

		private void SetNoDependenciesInstalledText()
		{
			if (_loadingControl is not null)
			{
				_loadingControl.ShowText("<size=125%>Please install ScoreSaber or BeatLeader!</size>", false);
			}
		}

		[UIAction("#post-parse")]
		private void PostParse()
		{
			// To set rich text, I have to iterate through all cells, set each cell to allow rich text and next time they will have it
			var leaderboardTableCells = _leaderboardTransform!.GetComponentsInChildren<LeaderboardTableCell>(true);

			foreach (var leaderboardTableCell in leaderboardTableCells)
			{
				leaderboardTableCell.transform.Find("PlayerName").GetComponent<CurvedTextMeshPro>().richText = true;
			}

			_loadingControl = _leaderboardTransform.GetComponentInChildren<LoadingControl>(true);

			var loadingContainer = _loadingControl.transform.Find("LoadingContainer");
			loadingContainer.gameObject.SetActive(true);
			_loadingControl.ShowLoading();

			_infoButtons = new List<Button>();

			// Change info button scales
			ChangeButtonScale(_button1!, 0.425f);
			ChangeButtonScale(_button2!, 0.425f);
			ChangeButtonScale(_button3!, 0.425f);
			ChangeButtonScale(_button4!, 0.425f);
			ChangeButtonScale(_button5!, 0.425f);
			ChangeButtonScale(_button6!, 0.425f);
			ChangeButtonScale(_button7!, 0.425f);
			ChangeButtonScale(_button8!, 0.425f);
			ChangeButtonScale(_button9!, 0.425f);
			ChangeButtonScale(_button10!, 0.425f);
			
			if (Utils.IsDependencyLeaderboardInstalled is false)
			{
				SetNoDependenciesInstalledText();
				
				foreach (var button in _infoButtons)
				{
					button.gameObject.SetActive(false);
				}
			}
		}

		[UIAction("up-clicked")]
		private void UpClicked()
		{
			if (UpEnabled)
			{
				PageNumber--;
			}
		}

		[UIAction("down-clicked")]
		private void DownClicked()
		{
			if (DownEnabled)
			{
				PageNumber++;
			}
		}

		#region Info Buttons

		[UIComponent("button1")]
		private readonly Button? _button1 = null!;

		[UIComponent("button2")]
		private readonly Button? _button2 = null!;

		[UIComponent("button3")]
		private readonly Button? _button3 = null!;

		[UIComponent("button4")]
		private readonly Button? _button4 = null!;

		[UIComponent("button5")]
		private readonly Button? _button5 = null!;

		[UIComponent("button6")]
		private readonly Button? _button6 = null!;

		[UIComponent("button7")]
		private readonly Button? _button7 = null!;

		[UIComponent("button8")]
		private readonly Button? _button8 = null!;

		[UIComponent("button9")]
		private readonly Button? _button9 = null!;

		[UIComponent("button10")]
		private readonly Button? _button10 = null!;

		#endregion

		#region Info Buttons Clicked

		[UIAction("b-1-click")]
		private void B1Clicked()
		{
			InfoButtonClicked(0);
		}

		[UIAction("b-2-click")]
		private void B2Clicked()
		{
			InfoButtonClicked(1);
		}

		[UIAction("b-3-click")]
		private void B3Clicked()
		{
			InfoButtonClicked(2);
		}

		[UIAction("b-4-click")]
		private void B4Clicked()
		{
			InfoButtonClicked(3);
		}

		[UIAction("b-5-click")]
		private void B5Clicked()
		{
			InfoButtonClicked(4);
		}

		[UIAction("b-6-click")]
		private void B6Clicked()
		{
			InfoButtonClicked(5);
		}

		[UIAction("b-7-click")]
		private void B7Clicked()
		{
			InfoButtonClicked(6);
		}

		[UIAction("b-8-click")]
		private void B8Clicked()
		{
			InfoButtonClicked(7);
		}

		[UIAction("b-9-click")]
		private void B9Clicked()
		{
			InfoButtonClicked(8);
		}

		[UIAction("b-10-click")]
		private void B10Clicked()
		{
			InfoButtonClicked(9);
		}

		#endregion

		#region Segmented Control

		private int _selectedCellIndex;

		private int SelectedCellIndex
		{
			get => _selectedCellIndex;
			set
			{
				_selectedCellIndex = value;
				PageNumber = 0;
			}
		}

		[UIAction("cell-selected")]
		private void OnCellSelected(SegmentedControl _, int index)
		{
			SelectedCellIndex = index;
		}

		[UIValue("cell-data")]
		private List<IconSegmentedControl.DataItem> CellData
		{
			get
			{
				var list = new List<IconSegmentedControl.DataItem>();
				foreach (var leaderboardSource in _leaderboardSources)
				{
					list.Add(new IconSegmentedControl.DataItem(leaderboardSource.Icon, leaderboardSource.HoverHint));
				}

				return list;
			}
		}

		#endregion
	}
}