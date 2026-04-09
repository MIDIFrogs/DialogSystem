using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;
using UnityEngine.UI;

namespace MIDIFrogs.DialogSystem.UI
{
    public class AnimatedStoryboardView : MonoBehaviour, IStoryboardView
    {
        [SerializeField] private Image image;

        [SerializeField, Min(0.1f)] private float fadeDuration = 0.5f;

        private CancellationTokenSource _cts;

        private void OnDisable()
        {
            _cts?.Cancel();
        }

        public void HideStoryboard()
        {
            _cts?.Cancel();
            gameObject.SetActive(false);
        }

        public async UniTask ShowStoryboard(IBackgroundImage background)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            if (background.TryGetImage(out Sprite newSprite) && newSprite != null)
            {
                gameObject.SetActive(true);

                if (image.sprite == null)
                {
                    image.sprite = newSprite;
                    image.color = new Color(1, 1, 1, 1);
                    return;
                }

                var overlay = new GameObject("Overlay", typeof(Image)).GetComponent<Image>();
                overlay.transform.SetParent(image.transform, false);
                overlay.sprite = newSprite;
                overlay.color = new Color(1, 1, 1, 0);
                overlay.raycastTarget = false;

                float t = 0f;
                while (t < 1f)
                {
                    if (token.IsCancellationRequested) break;
                    t += Time.unscaledDeltaTime / fadeDuration;
                    overlay.color = new Color(1, 1, 1, t);
                    image.color = new Color(1, 1, 1, 1 - t);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                image.sprite = newSprite;
                image.color = Color.white;
                Destroy(overlay.gameObject);
            }
        }

        public void SkipAnimation()
        {
            _cts?.Cancel();
            image.color = Color.white;
        }
    }
}
