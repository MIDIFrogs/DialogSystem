using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IDialogView
    {
        void Show();

        void Hide();

        /// <summary>
        /// Shows a line and its visible answers. Returns the picked answer, or null when the player continues a
        /// line without enabled answers.
        /// </summary>
        UniTask<DialogChoice> ShowLine(DialogNode node, IReadOnlyList<ChoiceState> choices, IDialogTextResolver text,
            CancellationToken token);
    }
}
