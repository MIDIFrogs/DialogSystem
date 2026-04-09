using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IDialogView
    {
        UniTask<DialogChoice> ShowNode(DialogNode node, IEnumerable<DialogChoice> choices);

        void Show();

        void Hide();
    }
}
