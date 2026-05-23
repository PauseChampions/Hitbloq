using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IPA.Loader;
using IPA.Utilities.Async;
using SiraUtil.Web;
using SiraUtil.Zenject;
using UnityEngine;

namespace Hitbloq.Other
{
	internal class SpriteLoader
	{
		private static readonly YieldInstruction LoadWait = new WaitForEndOfFrame();
		private static readonly ConcurrentDictionary<string, Sprite> CachedSprites = new ConcurrentDictionary<string, Sprite>();
		private static readonly ConcurrentDictionary<string, Task<Sprite>> InFlightSprites = new ConcurrentDictionary<string, Task<Sprite>>();
		private readonly PluginMetadata _pluginMetadata;
		private readonly object _queueLock;
		private readonly IHttpService _siraHttpService;
		private readonly Queue<Action> _spriteQueue;
#if !HITBLOQ_BS_1_29_1
		private readonly ICoroutineStarter _coroutineStarter;
#endif
		private bool _coroutineRunning;

#if HITBLOQ_BS_1_29_1
		public SpriteLoader(UBinder<Plugin, PluginMetadata> pluginMetadata, IHttpService siraHttpService)
#else
		public SpriteLoader(UBinder<Plugin, PluginMetadata> pluginMetadata, IHttpService siraHttpService, ICoroutineStarter coroutineStarter)
#endif
		{
			_pluginMetadata = pluginMetadata.Value;
			_siraHttpService = siraHttpService;
#if !HITBLOQ_BS_1_29_1
			_coroutineStarter = coroutineStarter;
#endif
			_spriteQueue = new Queue<Action>();
			_queueLock = new object();
		}

		public async Task FetchSpriteFromResourcesAsync(string spriteURL, Action<Sprite> onCompletion, CancellationToken cancellationToken = default)
		{
			if (CachedSprites.TryGetValue(spriteURL, out var cachedSprite))
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					onCompletion?.Invoke(cachedSprite);
				}

				return;
			}

			try
			{
				using var mrs = _pluginMetadata.Assembly.GetManifestResourceStream(spriteURL);
				using var ms = new MemoryStream();
				if (mrs != null)
				{
					await mrs.CopyToAsync(ms);
				}

				if (!cancellationToken.IsCancellationRequested)
				{
					var sprite = await QueueLoadSpriteAsync(spriteURL, ms.ToArray());
					if (!cancellationToken.IsCancellationRequested)
					{
						onCompletion?.Invoke(sprite);
					}
				}
			}
			catch (Exception)
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					onCompletion?.Invoke(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
				}
			}
		}

		public async Task DownloadSpriteAsync(string spriteURL, Action<Sprite> onCompletion, CancellationToken cancellationToken = default)
		{
			if (CachedSprites.TryGetValue(spriteURL, out var cachedSprite))
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					onCompletion?.Invoke(cachedSprite);
				}

				return;
			}

			try
			{
				var spriteTask = InFlightSprites.GetOrAdd(spriteURL, DownloadAndLoadSpriteAsync);
				var sprite = await spriteTask.ConfigureAwait(false);
				if (!cancellationToken.IsCancellationRequested)
				{
					onCompletion?.Invoke(sprite);
				}
			}
			catch (Exception)
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					onCompletion?.Invoke(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
				}
			}
		}

		public bool TryGetCachedSprite(string spriteURL, out Sprite sprite)
		{
			return CachedSprites.TryGetValue(spriteURL, out sprite);
		}

		private async Task<Sprite> DownloadAndLoadSpriteAsync(string spriteURL)
		{
			try
			{
				var webResponse = await _siraHttpService.GetAsync(spriteURL, cancellationToken: CancellationToken.None).ConfigureAwait(false);
				if (!webResponse.Successful)
				{
					return BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
				}

				var imageBytes = await webResponse.ReadAsByteArrayAsync();
				return await QueueLoadSpriteAsync(spriteURL, imageBytes).ConfigureAwait(false);
			}
			finally
			{
				InFlightSprites.TryRemove(spriteURL, out _);
			}
		}

		private Task<Sprite> QueueLoadSpriteAsync(string key, byte[] imageBytes)
		{
			if (CachedSprites.TryGetValue(key, out var cachedSprite))
			{
				return Task.FromResult(cachedSprite);
			}

			var taskCompletionSource = new TaskCompletionSource<Sprite>();
			var shouldStartCoroutine = false;
			lock (_queueLock)
			{
				_spriteQueue.Enqueue(async () =>
				{
					try
					{
#if HITBLOQ_BS_1_29_1
						var sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(imageBytes);
#else
						var sprite = await BeatSaberMarkupLanguage.Utilities.LoadSpriteAsync(imageBytes);
#endif
						sprite.texture.wrapMode = TextureWrapMode.Clamp;
						CachedSprites.TryAdd(key, sprite);
						taskCompletionSource.TrySetResult(sprite);
					}
					catch (Exception)
					{
						taskCompletionSource.TrySetResult(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
					}
				});

				if (!_coroutineRunning)
				{
					_coroutineRunning = true;
					shouldStartCoroutine = true;
				}
			}

			if (shouldStartCoroutine)
			{
#if HITBLOQ_BS_1_29_1
				_ = UnityMainThreadTaskScheduler.Factory.StartNew(ProcessSpriteQueue);
#else
				_coroutineStarter.StartCoroutine(SpriteLoadCoroutine());
#endif
			}

			return taskCompletionSource.Task;
		}

#if HITBLOQ_BS_1_29_1
		private void ProcessSpriteQueue()
		{
			while (true)
			{
				Action? loader = null;
				lock (_queueLock)
				{
					if (_spriteQueue.Count == 0)
					{
						_coroutineRunning = false;
						return;
					}

					loader = _spriteQueue.Dequeue();
				}

				loader?.Invoke();
			}
		}
#else
		private IEnumerator<YieldInstruction> SpriteLoadCoroutine()
		{
			while (true)
			{
				yield return LoadWait;

				Action? loader = null;
				lock (_queueLock)
				{
					if (_spriteQueue.Count == 0)
					{
						_coroutineRunning = false;
						yield break;
					}

					loader = _spriteQueue.Dequeue();
				}

				_ = UnityMainThreadTaskScheduler.Factory.StartNew(() => loader?.Invoke());
			}
		}
#endif
	}
}
