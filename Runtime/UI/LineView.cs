using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.View;
using TMPro;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>Shows the whole line at once.</summary>
    public class LineView : MonoBehaviour, ILineView
    {
        [SerializeField] private TMP_Text text;

        public bool IsTyping => false;

        public UniTask ShowText(StyledText value, string resolvedText, float? duration, Action<int> onCharacter, CancellationToken token)
        {
            LineStyle.Apply(text, value, resolvedText);
            text.maxVisibleCharacters = int.MaxValue;
            return UniTask.CompletedTask;
        }

        public void Complete()
        {
        }
    }
}
