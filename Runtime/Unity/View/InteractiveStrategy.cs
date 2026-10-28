using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    /// <summary>The player reads each line and picks answers.</summary>
    public class InteractiveStrategy : IDialogPlaybackStrategy
    {
        public UniTask<DialogChoice> Next(DialogNode node, IReadOnlyList<ChoiceState> choices, IDialogView view,
            IDialogTextResolver text, CancellationToken token) => view.ShowLine(node, choices, text, token);
    }
}
