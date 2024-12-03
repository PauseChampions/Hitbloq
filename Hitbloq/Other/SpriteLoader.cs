using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberPlaylistsLib;
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
		private readonly ConcurrentDictionary<string, Sprite> _cachedSprites;
		private readonly PluginMetadata _pluginMetadata;
		private readonly object _queueLock;
		private readonly IHttpService _siraHttpService;
		private readonly Queue<Action> _spriteQueue;
		private bool _coroutineRunning;

		public SpriteLoader(UBinder<Plugin, PluginMetadata> pluginMetadata, IHttpService siraHttpService)
		{
			_pluginMetadata = pluginMetadata.Value;
			_siraHttpService = siraHttpService;
			_cachedSprites = new ConcurrentDictionary<string, Sprite>();
			_spriteQueue = new Queue<Action>();
			_queueLock = new object();
		}

		public async Task FetchSpriteFromResourcesAsync(string spriteURL, Action<Sprite> onCompletion, CancellationToken cancellationToken = default)
		{
			// Check Cache
			if (_cachedSprites.TryGetValue(spriteURL, out var cachedSprite))
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
					QueueLoadSprite(spriteURL, ms.ToArray(), onCompletion, cancellationToken);
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
			// Check Cache
			if (_cachedSprites.TryGetValue(spriteURL, out var cachedSprite))
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					onCompletion?.Invoke(cachedSprite);
				}

				return;
			}

			try
			{
				var webResponse = await _siraHttpService.GetAsync(spriteURL, cancellationToken: cancellationToken).ConfigureAwait(false);
				if (webResponse.Successful)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						var imageBytes = await webResponse.ReadAsByteArrayAsync();
						QueueLoadSprite(spriteURL, imageBytes, onCompletion, cancellationToken);
					}
				}
				else if (!cancellationToken.IsCancellationRequested)
				{
					onCompletion?.Invoke(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
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

		private void QueueLoadSprite(string key, byte[] imageBytes, Action<Sprite> onCompletion, CancellationToken cancellationToken)
		{
			_spriteQueue.Enqueue(async () =>
			{
				try
				{
					var sprite = await BeatSaberMarkupLanguage.Utilities.LoadSpriteAsync(imageBytes);
					sprite.texture.wrapMode = TextureWrapMode.Clamp;
					_cachedSprites.TryAdd(key, sprite);
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
			});
			if (!_coroutineRunning)
			{
				SharedCoroutineStarter.instance?.StartCoroutine(SpriteLoadCoroutine());
			}
		}

		private IEnumerator<YieldInstruction> SpriteLoadCoroutine()
		{
			lock (_queueLock)
			{
				if (_coroutineRunning)
				{
					yield break;
				}

				_coroutineRunning = true;
			}

			while (_spriteQueue.Count > 0)
			{
				yield return LoadWait;
				if (_spriteQueue.Count == 0)
				{
					break;
				}

				var loader = _spriteQueue.Dequeue();
				_ = UnityMainThreadTaskScheduler.Factory.StartNew(() => loader?.Invoke());
			}

			_coroutineRunning = false;
			if (_spriteQueue.Count > 0)
			{
				SharedCoroutineStarter.instance?.StartCoroutine(SpriteLoadCoroutine());
			}
		}
	}
}