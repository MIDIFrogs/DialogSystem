using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IDialogPlaybackStrategy
    {
        /// <summary>Returns the picked answer, or null to continue.</summary>
        UniTask<DialogChoice> Next(DialogNode node, IReadOnlyList<ChoiceState> choices, IDialogView view,
            IDialogTextResolver text, CancellationToken token);
    }
}
