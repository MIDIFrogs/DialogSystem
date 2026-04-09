using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IDialogPlaybackStrategy
    {
        UniTask<DialogChoice> SelectChoice(DialogNode node, IEnumerable<DialogChoice> choices, IDialogView view);
    }
}
