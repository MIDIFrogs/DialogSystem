using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    /// <summary>
    /// Shows each line for a fixed time (unscaled). If the player acts first their pick wins; otherwise the view's
    /// wait is cancelled and the first enabled answer (or continue) is taken.
    /// </summary>
    public class AutoplayStrategy : IDialogPlaybackStrategy
    {
        private readonly float seconds;

        public AutoplayStrategy(float seconds)
        {
            this.seconds = seconds;
        }

        public async UniTask<DialogChoice> Next(DialogNode node, IReadOnlyList<ChoiceState> choices, IDialogView view,
            IDialogTextResolver text, CancellationToken token)
        {
            using var line = CancellationTokenSource.CreateLinkedTokenSource(token);
            // A completion source can be awaited twice: by WhenAny and, when the timer wins, after cancelling.
            var viewDone = new UniTaskCompletionSource<(bool cancelled, DialogChoice picked)>();
            RunView(view.ShowLine(node, choices, text, line.Token), viewDone).Forget();
            UniTask<(bool cancelled, DialogChoice picked)> shown = viewDone.Task;
            UniTask timer = UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime, cancellationToken: line.Token)
                .SuppressCancellationThrow();

            (bool viewWon, (bool cancelled, DialogChoice picked) result) = await UniTask.WhenAny(shown, timer);
            token.ThrowIfCancellationRequested();
            if (viewWon && !result.cancelled)
            {
                line.Cancel();
                return result.picked;
            }

            // Let the view clean up this line before the next one starts.
            line.Cancel();
            await shown;
            return SkipStrategy.FirstEnabled(choices);
        }

        private static async UniTaskVoid RunView(UniTask<DialogChoice> shown,
            UniTaskCompletionSource<(bool cancelled, DialogChoice picked)> done)
        {
            try
            {
                done.TrySetResult(await shown.SuppressCancellationThrow());
            }
            catch (Exception exception)
            {
                done.TrySetException(exception);
            }
        }
    }
}
