using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>
    /// Shows a line with its author, background, voice and answers. A line without enabled answers shows the
    /// continue prompt and waits for the dialog input or a click; while the text types, the first press completes it.
    /// Storyboard, sound player, input and prompt are optional.
    /// </summary>
    public class DialogRootView : MonoBehaviour, IDialogView
    {
        [Header("Views")]
        [SerializeField] private GameObject lineViewObject;
        [SerializeField] private GameObject authorViewObject;
        [SerializeField] private GameObject choicesViewObject;

        [Header("Optional")]
        [SerializeField] private GameObject storyboardViewObject;
        [SerializeField] private GameObject soundPlayerObject;
        [SerializeField] private GameObject inputObject;
        [SerializeField] private ContinuePrompt continuePrompt;

        [Header("Typing sound")]
        [Tooltip("Used when the author has no typing sound.")]
        [SerializeReference, DialogSound] private IDialogSound defaultTypingSound;
        [SerializeField, Min(1)] private int defaultBlipEvery = 2;
        [SerializeField, Range(0f, 0.5f)] private float defaultPitchVariance = 0.05f;

        private ILineView lineView;
        private IAuthorView authorView;
        private IChoicesView choicesView;
        private IStoryboardView storyboardView;
        private IDialogSoundPlayer soundPlayer;
        private IDialogInput input;

        public void Show() => gameObject.SetActive(true);

        public void Hide()
        {
            // The dialog can end because this view was destroyed with its scene.
            if (this == null)
            {
                return;
            }

            soundPlayer?.StopVoice();
            choicesView?.Clear();
            if (continuePrompt != null)
            {
                continuePrompt.Hide();
            }
            gameObject.SetActive(false);
        }

        public async UniTask<DialogChoice> ShowLine(DialogNode node, IReadOnlyList<ChoiceState> choices,
            IDialogTextResolver text, CancellationToken token)
        {
            Resolve();
            text ??= SourceTextResolver.Instance;
            if (node.Author != null)
            {
                authorView?.ShowAuthor(node.Author, text);
            }
            else
            {
                authorView?.HideAuthor();
            }

            var voice = node.Voice as IDialogSound;
            soundPlayer?.PlayVoice(voice);
            storyboardView?.ShowStoryboard(node.BackgroundImage).Forget();
            float? duration = voice != null && voice.Length > 0f ? voice.Length : null;
            UniTask typing = lineView.ShowText(node.Text, text.Resolve(node.Text.Key, node.Text.Content), duration,
                voice == null ? Blipper(node.Author) : null, token);

            using var lineToken = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                bool anyEnabled = false;
                foreach (ChoiceState state in choices)
                {
                    anyEnabled |= state.Enabled;
                }

                if (anyEnabled)
                {
                    return await choicesView.ShowChoices(choices, text, lineToken.Token);
                }

                if (choices.Count > 0)
                {
                    // Locked answers only: show them with their hints, continue like a linear line.
                    choicesView.ShowChoices(choices, text, lineToken.Token).SuppressCancellationThrow().Forget();
                }
                await WaitForContinue(text, token);
                return null;
            }
            finally
            {
                lineToken.Cancel();
                typing.Forget();
                if (this != null)
                {
                    choicesView.Clear();
                    lineView.Complete();
                    storyboardView?.SkipAnimation();
                    soundPlayer?.StopVoice();
                    if (continuePrompt != null)
                    {
                        continuePrompt.Hide();
                    }
                }
            }
        }

        private async UniTask WaitForContinue(IDialogTextResolver text, CancellationToken token)
        {
            if (continuePrompt != null)
            {
                continuePrompt.Show(text);
            }

            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                bool pressed = (input?.AdvancePressedThisFrame() ?? false) | (continuePrompt != null && continuePrompt.ConsumeClick());
                if (!pressed)
                {
                    continue;
                }

                if (lineView.IsTyping)
                {
                    lineView.Complete();
                    continue;
                }
                return;
            }
        }

        private System.Action<int> Blipper(DialogAuthor author)
        {
            if (soundPlayer == null)
            {
                return null;
            }

            var asset = author?.Source as DialogAuthorAsset;
            IDialogSound sound = asset != null && asset.TypingSound != null ? asset.TypingSound : defaultTypingSound;
            if (sound == null)
            {
                return null;
            }

            int every = asset != null ? asset.BlipEvery : defaultBlipEvery;
            float variance = asset != null ? asset.PitchVariance : defaultPitchVariance;
            return shown =>
            {
                if (shown % Mathf.Max(1, every) == 0)
                {
                    soundPlayer.Blip(sound, variance);
                }
            };
        }

        private void Resolve()
        {
            if (lineView != null)
            {
                return;
            }

            lineView = lineViewObject.GetComponent<ILineView>();
            authorView = authorViewObject != null ? authorViewObject.GetComponent<IAuthorView>() : null;
            choicesView = choicesViewObject.GetComponent<IChoicesView>();
            storyboardView = storyboardViewObject != null ? storyboardViewObject.GetComponent<IStoryboardView>() : null;
            soundPlayer = soundPlayerObject != null ? soundPlayerObject.GetComponent<IDialogSoundPlayer>() : null;
            input = inputObject != null ? inputObject.GetComponent<IDialogInput>() : null;
        }
    }
}
