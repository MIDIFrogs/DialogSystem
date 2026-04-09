using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IChoicesView
    {
        UniTask<DialogChoice> ShowChoices(IEnumerable<DialogChoice> choices);
        void Clear();
    }
}
