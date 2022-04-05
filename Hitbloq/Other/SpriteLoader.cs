using SiraUtil.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Hitbloq.Other
{
    internal class SpriteLoader
    {
        private readonly IHttpService siraHttpService;
        private readonly Dictionary<string, Sprite> cachedURLSprites;
        private readonly ConcurrentQueue<Action> spriteQueue;

        public SpriteLoader(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
            cachedURLSprites = new Dictionary<string, Sprite>();
            spriteQueue = new ConcurrentQueue<Action>();
        }

        public async void DownloadSpriteAsync(string spriteURL, Action<Sprite> onCompletion)
        {
            // Check Cache
            if (cachedURLSprites.TryGetValue(spriteURL, out var cachedSprite))
            {
                onCompletion?.Invoke(cachedSprite);
                return;
            }

            try
            {
                var webResponse = await siraHttpService.GetAsync(spriteURL, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                var imageBytes = await webResponse.ReadAsByteArrayAsync();
                QueueLoadSprite(spriteURL, cachedURLSprites, imageBytes, onCompletion);
            }
            catch (Exception)
            {
                onCompletion?.Invoke(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
            }
        }

        private void QueueLoadSprite(string key, Dictionary<string, Sprite> cache, byte[] imageBytes, Action<Sprite> onCompletion)
        {
            spriteQueue.Enqueue(() =>
            {
                try
                {
                    var sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(imageBytes);
                    sprite.texture.wrapMode = TextureWrapMode.Clamp;
                    cache[key] = sprite;
                    onCompletion?.Invoke(sprite);
                }
                catch (Exception)
                {
                    onCompletion?.Invoke(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
                }
            });
            SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
        }

        public static YieldInstruction LoadWait = new WaitForEndOfFrame();

        private IEnumerator<YieldInstruction> SpriteLoadCoroutine()
        {
            while (spriteQueue.TryDequeue(out var loader))
            {
                yield return LoadWait;
                loader?.Invoke();
            }
        }
    }
}