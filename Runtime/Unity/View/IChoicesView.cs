using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IChoicesView
    {
        /// <summary>Shows the answers (locked ones with their hint) and completes when an enabled one is picked.</summary>
        UniTask<DialogChoice> ShowChoices(IReadOnlyList<ChoiceState> choices, IDialogTextResolver text, CancellationToken token);

        void Clear();
    }
}
