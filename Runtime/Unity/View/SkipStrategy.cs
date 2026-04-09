using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public class SkipStrategy : IDialogPlaybackStrategy
    {
        public UniTask<DialogChoice> SelectChoice(DialogNode node, IEnumerable<DialogChoice> choices, IDialogView view) => UniTask.FromResult(choices.FirstOrDefault());
    }
}
