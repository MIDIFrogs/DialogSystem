using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface ILineView
    {
        bool IsTyping { get; }

        /// <summary>
        /// Shows <paramref name="resolvedText"/> styled by <paramref name="text"/>. A duration (e.g. the voice length)
        /// spreads the typing over it; <paramref name="onCharacter"/> gets the count of characters shown so far.
        /// </summary>
        UniTask ShowText(StyledText text, string resolvedText, float? duration, Action<int> onCharacter, CancellationToken token);

        /// <summary>Shows the whole text at once.</summary>
        void Complete();
    }
}
