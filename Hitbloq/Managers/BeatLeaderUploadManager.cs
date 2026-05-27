#if !HITBLOQ_BS_1_29_1
using System;
using System.Linq.Expressions;
using System.Reflection;
using Zenject;

namespace Hitbloq.Managers
{
	internal sealed class BeatLeaderUploadManager : IInitializable, IDisposable
	{
		private readonly HitbloqManager _hitbloqManager;
		private EventInfo? _newUploadStateChangedEvent;
		private FieldInfo? _legacyReplayUploadStartedField;
		private Delegate? _uploadEventHandler;

		public BeatLeaderUploadManager(HitbloqManager hitbloqManager)
		{
			_hitbloqManager = hitbloqManager;
		}

		public void Dispose()
		{
			// Edited by GPT-5 Codex 2026-05-27
			// BeatLeader changed upload APIs between releases, so unsubscribe through reflection.
			// This keeps old BeatLeader builds loadable when newer web request types are absent.
			if (_uploadEventHandler == null)
			{
				return;
			}

			_newUploadStateChangedEvent?.RemoveEventHandler(null, _uploadEventHandler);
			if (_legacyReplayUploadStartedField != null)
			{
				var currentDelegate = _legacyReplayUploadStartedField.GetValue(null) as Delegate;
				_legacyReplayUploadStartedField.SetValue(null, Delegate.Remove(currentDelegate, _uploadEventHandler));
			}
		}

		public void Initialize()
		{
			// Edited by GPT-5 Codex 2026-05-27
			// New BeatLeader exposes an upload state event; old BeatLeader exposes replay upload start.
			// Subscribe to whichever API exists without putting missing types in this class signature.
			if (TrySubscribeNewUploadEvent())
			{
				return;
			}

			TrySubscribeLegacyUploadEvent();
		}

		private bool TrySubscribeNewUploadEvent()
		{
			var uploadRequestType = Type.GetType("BeatLeader.API.UploadReplayRequest, BeatLeader", false);
			_newUploadStateChangedEvent = uploadRequestType?.GetEvent("StateChangedEvent", BindingFlags.Public | BindingFlags.Static);
			if (_newUploadStateChangedEvent?.EventHandlerType == null)
			{
				return false;
			}

			_uploadEventHandler = CreateNewUploadEventHandler(_newUploadStateChangedEvent.EventHandlerType);
			_newUploadStateChangedEvent.AddEventHandler(null, _uploadEventHandler);
			return true;
		}

		private bool TrySubscribeLegacyUploadEvent()
		{
			var scoreUtilType = Type.GetType("BeatLeader.Utils.ScoreUtil, BeatLeader", false);
			_legacyReplayUploadStartedField = scoreUtilType?.GetField("ReplayUploadStartedEvent", BindingFlags.Public | BindingFlags.Static);
			var eventHandlerType = _legacyReplayUploadStartedField?.FieldType;
			if (eventHandlerType == null)
			{
				return false;
			}

			_uploadEventHandler = CreateLegacyUploadEventHandler(eventHandlerType);
			var currentDelegate = _legacyReplayUploadStartedField!.GetValue(null) as Delegate;
			_legacyReplayUploadStartedField.SetValue(null, Delegate.Combine(currentDelegate, _uploadEventHandler));
			return true;
		}

		private Delegate CreateNewUploadEventHandler(Type eventHandlerType)
		{
			var invokeMethod = eventHandlerType.GetMethod("Invoke");
			var parameters = invokeMethod!.GetParameters();
			var requestParameter = Expression.Parameter(parameters[0].ParameterType, parameters[0].Name);
			var stateParameter = Expression.Parameter(parameters[1].ParameterType, parameters[1].Name);
			var failReasonParameter = Expression.Parameter(parameters[2].ParameterType, parameters[2].Name);
			var body = Expression.Call(
				Expression.Constant(this),
				nameof(OnNewUploadStateChanged),
				null,
				Expression.Convert(requestParameter, typeof(object)),
				Expression.Convert(stateParameter, typeof(object)),
				failReasonParameter);

			return Expression.Lambda(eventHandlerType, body, requestParameter, stateParameter, failReasonParameter).Compile();
		}

		private Delegate CreateLegacyUploadEventHandler(Type eventHandlerType)
		{
			var invokeMethod = eventHandlerType.GetMethod("Invoke");
			var replayParameterType = invokeMethod!.GetParameters()[0].ParameterType;
			var replayParameter = Expression.Parameter(replayParameterType, "replay");
			var body = Expression.Call(
				Expression.Constant(this),
				nameof(OnLegacyReplayUploadStarted),
				null,
				Expression.Convert(replayParameter, typeof(object)));

			return Expression.Lambda(eventHandlerType, body, replayParameter).Compile();
		}

		private void OnNewUploadStateChanged(object request, object state, string? failReason)
		{
			// Edited by GPT-5 Codex 2026-05-27
			// New BeatLeader can report attempts/errors through the upload event.
			// Refresh only when the state finished and the response status is Uploaded.
			if (state.ToString() == "Finished" && IsUploadedStatus(request))
			{
				_hitbloqManager.OnScoreUploaded();
			}
		}

		private void OnLegacyReplayUploadStarted(object replay)
		{
			// Edited by GPT-5 Codex 2026-05-27
			// Old BeatLeader lacks the newer upload response, so use replay completion data.
			// Only a cleared play can refresh Hitbloq; quits/fails/practice replays are ignored.
			if (IsClearedReplay(replay))
			{
				_hitbloqManager.OnScoreUploaded();
			}
		}

		private static bool IsUploadedStatus(object request)
		{
			var result = request.GetType().GetProperty("Result")?.GetValue(request);
			var status = result?.GetType().GetField("Status")?.GetValue(result);
			return status?.ToString() == "Uploaded";
		}

		private static bool IsClearedReplay(object replay)
		{
			var info = replay.GetType().GetField("info")?.GetValue(replay);
			var levelEndType = info?.GetType().GetField("levelEndType")?.GetValue(info)
			                   ?? info?.GetType().GetProperty("LevelEndType")?.GetValue(info);
			return levelEndType?.ToString() == "Clear";
		}
	}
}
#endif
