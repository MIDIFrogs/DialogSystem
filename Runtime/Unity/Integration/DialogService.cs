using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.View;

namespace MIDIFrogs.DialogSystem.Unity.Integration
{
    public class DialogService
    {
        private readonly IDialogView view;
        private IDialogContext context;

        private CancellationTokenSource flowControlSource;

        private DialogRunner currentRunner;

        public IDialogPlaybackStrategy PlaybackStrategy { get; set; } = new InteractiveStrategy();

        public bool IsPlaying => currentRunner != null;

        public DialogService(IDialogView view, IDialogContext context) : this(view)
        {
            this.context = context;
        }

        public DialogService(IDialogView view)
        {
            this.view = view;
            this.context = new DialogContext();
        }

        public void SetContext(IDialogContext context) => this.context = context;

        public async UniTask Play(Dialog dialog, CancellationToken token = default)
        {
            if (IsPlaying)
                return;

            flowControlSource = new CancellationTokenSource();

            currentRunner = new DialogRunner(context);
            currentRunner.Start(dialog);

            view.Show();

            try
            {
                while (!currentRunner.IsFinished
                    && !flowControlSource.IsCancellationRequested
                    && !token.IsCancellationRequested)
                {
                    var node = currentRunner.CurrentNode;
                    var choices = currentRunner.GetAvailableChoices();

                    var selected = await PlaybackStrategy.SelectChoice(node, choices, view)
                        .AttachExternalCancellation(flowControlSource.Token)
                        .AttachExternalCancellation(token);

                    currentRunner.Choose(selected);
                }
            }
            finally
            {
                view.Hide();
                currentRunner = null;
            }
        }

        public void Stop() => flowControlSource?.Cancel();
    }
}
