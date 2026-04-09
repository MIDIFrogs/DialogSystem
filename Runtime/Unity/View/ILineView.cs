using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface ILineView
    {
        UniTask ShowText(StyledText text, float? duration = null);
        void Skip();
    }
}
