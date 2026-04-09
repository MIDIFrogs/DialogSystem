using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Unity.View;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
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
        private IVoicePlayer voicePlayer;
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
            if (dialogInput != null && dialogInput.WasSkipPressedThisFrame())
            {
                lineView.Skip();
                voicePlayer?.Stop();
                storyboardView?.SkipAnimation();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            voicePlayer?.Stop();
            gameObject.SetActive(false);
        }

        public async UniTask<DialogChoice> ShowNode(
            DialogNode node,
            IEnumerable<DialogChoice> choices)
        {
            SetupAuthor(node);

            voicePlayer?.Play(node.Voice);
            var text = lineView.ShowText(node.Text, node.Voice?.Duration);
            var background = storyboardView.ShowStoryboard(node.BackgroundImage);
            var choice = choicesView.ShowChoices(choices);

            var selected = await choice;
            lineView.Skip();
            storyboardView.SkipAnimation();
            voicePlayer.Stop();

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
