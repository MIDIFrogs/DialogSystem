using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;
using UnityEngine.UI;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>Cross-fades to the new background on unscaled time. A skipped fade jumps to the new image.</summary>
    public class AnimatedStoryboardView : MonoBehaviour, IStoryboardView
    {
        [SerializeField] private Image image;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.5f;

        private CancellationTokenSource cts;

        public void HideStoryboard()
        {
            cts?.Cancel();
            gameObject.SetActive(false);
        }

        public async UniTask ShowStoryboard(IBackgroundImage background)
        {
            if (background == null || !background.TryGetImage(out Sprite newSprite) || newSprite == null)
            {
                return;
            }

            cts?.Cancel();
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            gameObject.SetActive(true);

            if (image.sprite == null)
            {
                image.sprite = newSprite;
                image.color = Color.white;
                return;
            }

            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            overlay.transform.SetParent(image.transform, false);
            var rect = (RectTransform)overlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlay.sprite = newSprite;
            overlay.color = new Color(1f, 1f, 1f, 0f);
            overlay.raycastTarget = false;

            try
            {
                float t = 0f;
                while (t < 1f)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    t = Mathf.Min(1f, t + Time.unscaledDeltaTime / fadeDuration);
                    overlay.color = new Color(1f, 1f, 1f, t);
                    image.color = new Color(1f, 1f, 1f, 1f - t);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (image != null)
                {
                    image.sprite = newSprite;
                    image.color = Color.white;
                }
                if (overlay != null)
                {
                    overlay.transform.SetParent(null, false);
                    Destroy(overlay.gameObject);
                }
            }
        }

        public void SkipAnimation() => cts?.Cancel();

        private void OnDisable() => cts?.Cancel();
    }
}
