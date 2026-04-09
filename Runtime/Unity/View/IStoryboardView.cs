using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IStoryboardView
    {
        UniTask ShowStoryboard(IBackgroundImage background);

        void SkipAnimation();

        void HideStoryboard();
    }
}
