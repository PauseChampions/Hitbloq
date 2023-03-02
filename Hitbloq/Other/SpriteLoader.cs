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
        private readonly PluginMetadata pluginMetadata;
        private readonly IHttpService siraHttpService;
        private readonly ConcurrentDictionary<string, Sprite> cachedSprites;
        private readonly Queue<Action> spriteQueue;
        private readonly object queueLock;
        private bool coroutineRunning;

        public SpriteLoader(UBinder<Plugin, PluginMetadata> pluginMetadata, IHttpService siraHttpService)
        {
            this.pluginMetadata = pluginMetadata.Value;
            this.siraHttpService = siraHttpService;
            cachedSprites = new ConcurrentDictionary<string, Sprite>();
            spriteQueue = new Queue<Action>();
            queueLock = new object();
        }

        public async Task FetchSpriteFromResourcesAsync(string spriteURL, Action<Sprite> onCompletion, CancellationToken cancellationToken = default)
        {
            // Check Cache
            if (cachedSprites.TryGetValue(spriteURL, out var cachedSprite))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    onCompletion?.Invoke(cachedSprite);
                }
                return;
            }

            try
            {
                using var mrs = pluginMetadata.Assembly.GetManifestResourceStream(spriteURL);
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
            if (cachedSprites.TryGetValue(spriteURL, out var cachedSprite))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    onCompletion?.Invoke(cachedSprite);
                }
                return;
            }

            try
            {
                var webResponse = await siraHttpService.GetAsync(spriteURL, cancellationToken: cancellationToken).ConfigureAwait(false);
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
            spriteQueue.Enqueue(() =>
            {
                try
                {
                    var sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(imageBytes);
                    sprite.texture.wrapMode = TextureWrapMode.Clamp;
                    cachedSprites.TryAdd(key, sprite);
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
            if (!coroutineRunning)
            {
                SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
            }
        }

        private static readonly YieldInstruction LoadWait = new WaitForEndOfFrame();
        private IEnumerator<YieldInstruction> SpriteLoadCoroutine()
        {
            lock (queueLock)
            {
                if (coroutineRunning)
                    yield break;
                coroutineRunning = true;
            }
            while (spriteQueue.Count > 0)
            {
                yield return LoadWait;
                if (spriteQueue.Count == 0)
                {
                    break;
                }
                var loader = spriteQueue.Dequeue();
                _ = UnityMainThreadTaskScheduler.Factory.StartNew(() => loader?.Invoke());
            }
            coroutineRunning = false;
            if (spriteQueue.Count > 0)
            {
                SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
            }
        }
    }
}