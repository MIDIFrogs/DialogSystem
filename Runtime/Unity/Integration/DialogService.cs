using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Unity.View;

namespace MIDIFrogs.DialogSystem.Unity.Integration
{
    /// <summary>
    /// Plays a dialog on a view. The caller owns the context: load game state into it before, read it back after.
    /// <see cref="Stop"/> ends <see cref="Play"/> normally; the caller's own token still cancels it with an exception.
    /// </summary>
    public class DialogService
    {
        private readonly IDialogView view;
        private CancellationTokenSource stopSource;

        public DialogService(IDialogView view)
        {
            this.view = view;
        }

        public IDialogTextResolver Text { get; set; } = SourceTextResolver.Instance;

        public DialogFunctions Functions { get; } = new();

        public IDialogPlaybackStrategy PlaybackStrategy { get; set; } = new InteractiveStrategy();

        public bool IsPlaying { get; private set; }

        public async UniTask Play(Dialog dialog, IDialogContext context, CancellationToken token = default)
        {
            if (IsPlaying)
            {
                return;
            }

            IsPlaying = true;
            stopSource = new CancellationTokenSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, stopSource.Token);
            try
            {
                var runner = new DialogRunner(context, Functions, Text);
                runner.Start(dialog);
                view.Show();
                while (!runner.IsFinished)
                {
                    DialogChoice picked;
                    try
                    {
                        picked = await PlaybackStrategy.Next(runner.CurrentNode, runner.Choices, view, Text, linked.Token);
                    }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (picked == null || !runner.IsWaitingForChoice)
                    {
                        runner.Advance();
                    }
                    else
                    {
                        runner.Choose(picked);
                    }
                }
            }
            finally
            {
                view.Hide();
                stopSource.Dispose();
                stopSource = null;
                IsPlaying = false;
            }
        }

        public void Stop() => stopSource?.Cancel();
    }
}
