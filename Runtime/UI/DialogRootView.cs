using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MIDIFrogs.DialogSystem.Unity.View;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    public class DialogRootView : MonoBehaviour, IDialogView
    {
        [Header("Sub Views")]
        [SerializeField] private GameObject lineViewObject;
        [SerializeField] private GameObject authorViewObject;
        [SerializeField] private GameObject choicesViewObject;

        [Header("Optional")]
        [SerializeField] private GameObject storyboardViewObject;
        [SerializeField] private GameObject voicePlayerObject;
        [SerializeField] private GameObject dialogInputObject;

        private ILineView lineView;
        private IAuthorView authorView;
        private IChoicesView choicesView;
        private IStoryboardView storyboardView;
        private IDialogSoundPlayer voicePlayer;
        private IDialogInput dialogInput;

        private void Awake()
        {
            lineView = lineViewObject.GetComponent<ILineView>();
            authorView = authorViewObject.GetComponent<IAuthorView>();
            choicesView = choicesViewObject.GetComponent<IChoicesView>();
            if (storyboardViewObject != null) storyboardViewObject.TryGetComponent(out storyboardView);
            if (voicePlayerObject != null) voicePlayerObject.TryGetComponent(out voicePlayer);
            if (dialogInputObject != null) dialogInputObject.TryGetComponent(out dialogInput);
        }

        private void Update()
        {
            if (dialogInput != null && dialogInput.AdvancePressedThisFrame())
            {
                lineView.Skip();
                voicePlayer?.StopVoice();
                storyboardView?.SkipAnimation();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            voicePlayer?.StopVoice();
            gameObject.SetActive(false);
        }

        public async UniTask<DialogChoice> ShowLine(DialogNode node, IReadOnlyList<ChoiceState> states,
            IDialogTextResolver resolver, CancellationToken token)
        {
            var choices = states.Where(state => state.Enabled).Select(state => state.Choice).ToList();
            if (choices.Count == 0)
            {
                SetupAuthor(node);
                await lineView.ShowText(node.Text, null);
                return null;
            }

            SetupAuthor(node);

            voicePlayer?.PlayVoice(node.Voice as IDialogSound);
            var text = lineView.ShowText(node.Text, (node.Voice as IDialogSound)?.Length);
            var background = storyboardView.ShowStoryboard(node.BackgroundImage);
            var choice = choicesView.ShowChoices(choices);

            var selected = await choice;
            lineView.Skip();
            storyboardView.SkipAnimation();
            voicePlayer?.StopVoice();

            return selected;
        }

        private void SetupAuthor(DialogNode node)
        {
            if (node.Author != null)
                authorView.ShowAuthor(node.Author);
            else
                authorView.HideAuthor();
        }
    }
}
