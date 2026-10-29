using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.View;
using TMPro;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>Reveals the line character by character on unscaled time, keeping rich-text tags intact.</summary>
    public class TypewriterLineView : MonoBehaviour, ILineView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField, Min(1f)] private float charactersPerSecond = 40f;

        private int total;
        private bool complete;
        private int run;

        public bool IsTyping { get; private set; }

        public async UniTask ShowText(StyledText value, string resolvedText, float? duration, Action<int> onCharacter,
            CancellationToken token)
        {
            int current = ++run;
            LineStyle.Apply(text, value, resolvedText);
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            total = text.textInfo.characterCount;
            complete = false;
            IsTyping = total > 0;

            float speed = duration is > 0f ? total / duration.Value : charactersPerSecond;
            float shown = 0f;
            int visible = 0;
            try
            {
                while (visible < total && !complete)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    if (current != run)
                    {
                        return;
                    }

                    shown += Time.unscaledDeltaTime * speed;
                    int next = Mathf.Min(total, Mathf.FloorToInt(shown));
                    if (next == visible)
                    {
                        continue;
                    }

                    visible = next;
                    text.maxVisibleCharacters = visible;
                    onCharacter?.Invoke(visible);
                }
            }
            finally
            {
                // A newer ShowText owns the text now.
                if (current == run && text != null)
                {
                    text.maxVisibleCharacters = total;
                    IsTyping = false;
                }
            }
        }

        public void Complete()
        {
            complete = true;
            text.maxVisibleCharacters = total;
            IsTyping = false;
        }
    }
}
