using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public class InteractiveStrategy : IDialogPlaybackStrategy
    {
        public async UniTask<DialogChoice> SelectChoice(DialogNode node, IEnumerable<DialogChoice> choices, IDialogView view) => await view.ShowNode(node, choices);
    }
}
