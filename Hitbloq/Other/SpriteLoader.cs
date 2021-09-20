using SiraUtil;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Hitbloq.Other
{
    internal class SpriteLoader
    {
        private readonly SiraClient siraClient;
        private readonly Dictionary<string, Sprite> cachedSprites;

        private readonly Queue<Action> spriteQueue;
        private readonly object loaderLock;
        private bool coroutineRunning;

        public SpriteLoader(SiraClient siraClient)
        {
            this.siraClient = siraClient;
            cachedSprites = new Dictionary<string, Sprite>();

            spriteQueue = new Queue<Action>();
            loaderLock = new object();
            coroutineRunning = false;
        }

        public async void DownloadSpriteAsync(string spriteURL, Action<Sprite> onCompletion)
        {
            // Check Cache
            if (cachedSprites.TryGetValue(spriteURL, out Sprite cachedSprite))
            {
                onCompletion?.Invoke(cachedSprite);
                return;
            }

            try
            {
                WebResponse webResponse = await siraClient.GetAsync(spriteURL, CancellationToken.None).ConfigureAwait(false);
                byte[] imageBytes = webResponse.ContentToBytes();
                QueueLoadSprite(spriteURL, imageBytes, onCompletion);
            }
            catch (Exception)
            {
                onCompletion?.Invoke(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
            }
        }

        private void QueueLoadSprite(string spriteURL, byte[] imageBytes, Action<Sprite> onCompletion)
        {
            spriteQueue.Enqueue(() =>
            {
                try
                {
                    Sprite sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(imageBytes);
                    cachedSprites[spriteURL] = sprite;
                    onCompletion?.Invoke(sprite);
                }
                catch (Exception)
                {
                    onCompletion?.Invoke(BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
                }
            });

            if (!coroutineRunning)
            {
                SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
            }
        }

        public static YieldInstruction LoadWait = new WaitForEndOfFrame();

        private IEnumerator<YieldInstruction> SpriteLoadCoroutine()
        {
            lock (loaderLock)
            {
                if (coroutineRunning)
                    yield break;
                coroutineRunning = true;
            }

            while (spriteQueue.Count > 0)
            {
                yield return LoadWait;
                var loader = spriteQueue.Dequeue();
                loader?.Invoke();
            }

            coroutineRunning = false;
            if (spriteQueue.Count > 0)
            {
                SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
            }
        }
    }
}
