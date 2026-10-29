using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    /// <summary>Plays through without showing anything: the first enabled answer, otherwise continue.</summary>
    public class SkipStrategy : IDialogPlaybackStrategy
    {
        public UniTask<DialogChoice> Next(DialogNode node, IReadOnlyList<ChoiceState> choices, IDialogView view,
            IDialogTextResolver text, CancellationToken token) => UniTask.FromResult(FirstEnabled(choices));

        internal static DialogChoice FirstEnabled(IReadOnlyList<ChoiceState> choices)
        {
            foreach (ChoiceState state in choices)
            {
                if (state.Enabled)
                {
                    return state.Choice;
                }
            }
            return null;
        }
    }
}
