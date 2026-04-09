using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public class AutoplayStrategy : IDialogPlaybackStrategy
    {
        private readonly float waitTime;

        public AutoplayStrategy(float delay) => waitTime = delay;

        public async UniTask<DialogChoice> SelectChoice(DialogNode node, IEnumerable<DialogChoice> choices, IDialogView view)
        {
            var viewTask = view.ShowNode(node, choices);
            var skipTask = UniTask.Delay(TimeSpan.FromSeconds(waitTime), true);
            var (hasResult, result) = await UniTask.WhenAny(viewTask, skipTask);
            return hasResult ? result : choices.First();
        }
    }
}
