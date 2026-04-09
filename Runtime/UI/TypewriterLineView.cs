using System;
using System.Threading;
using MIDIFrogs.DialogSystem.Unity.View;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using TMPro;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    public class TypewriterLineView : MonoBehaviour, ILineView
    {
        [SerializeField] private TMP_Text text;

        [Header("Typewriter")]
        [SerializeField] private float charactersPerSecond = 40f;

        private CancellationTokenSource cts;
        private string currentText;
        private bool isAnimating;

        public async UniTask ShowText(StyledText value, float? duration = null)
        {
            CancelCurrent();

            currentText = value.Content;
            text.fontStyle = (FontStyles)value.Style;
            text.color = value.DefaultColor;
            text.text = string.Empty;

            cts = new CancellationTokenSource();
            var token = cts.Token;

            isAnimating = true;

            try
            {
                await TypeText(value.Content, duration, token);
            }
            catch (OperationCanceledException)
            {
            }

            isAnimating = false;
        }

        public void Skip()
        {
            if (!isAnimating)
                return;

            CancelCurrent();
            text.text = currentText;
            isAnimating = false;
        }

        private async UniTask TypeText(string value, float? requestedDuration, CancellationToken token)
        {
            float typeSpeed = requestedDuration == null || requestedDuration == 0 ? charactersPerSecond : value.Length / requestedDuration.Value;
            
            float delay = 1f / typeSpeed;

            for (int i = 0; i < value.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                text.text += value[i];

                await UniTask.Delay(
                    Mathf.CeilToInt(delay * 1000),
                    ignoreTimeScale: true,
                    cancellationToken: token);
            }
        }

        private void CancelCurrent()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }
}
