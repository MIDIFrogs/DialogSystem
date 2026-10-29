using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>
    /// One answer. A locked answer stays selectable (so keyboard and gamepad can reach it and read its hint) but
    /// is drawn faded and ignores presses; its "Требуется:" hint shows while it is selected or hovered.
    /// </summary>
    public class ResponseButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Button button;
        [SerializeField] private GameObject hintRoot;
        [SerializeField] private TMP_Text hintText;
        [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.45f;

        private UniTaskCompletionSource<DialogChoice> picked;
        private bool locked;
        private int boundFrame;

        public Button Button => button;

        public UniTask<DialogChoice> Bind(ChoiceState state, IDialogTextResolver resolver)
        {
            resolver ??= SourceTextResolver.Instance;
            text.text = resolver.Resolve(state.Choice.TextKey, state.Choice.Text);
            locked = !state.Enabled;
            Color color = text.color;
            color.a = locked ? lockedAlpha : 1f;
            text.color = color;

            if (hintText != null)
            {
                hintText.text = locked ? Hint(state.Hint, resolver) : string.Empty;
            }
            ShowHint(false);

            picked = new UniTaskCompletionSource<DialogChoice>();
            boundFrame = Time.frameCount;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // A Submit pressed in the frame the answers appear belongs to the previous line.
                if (!locked && Time.frameCount != boundFrame)
                {
                    picked.TrySetResult(state.Choice);
                }
            });
            return picked.Task;
        }

        public void OnSelect(BaseEventData eventData) => ShowHint(locked);

        public void OnDeselect(BaseEventData eventData) => ShowHint(false);

        public void OnPointerEnter(PointerEventData eventData) => ShowHint(locked);

        public void OnPointerExit(PointerEventData eventData) => ShowHint(false);

        private void ShowHint(bool visible)
        {
            if (hintRoot != null)
            {
                hintRoot.SetActive(visible);
            }
        }

        private static string Hint(IReadOnlyList<string> lines, IDialogTextResolver resolver) =>
            resolver.Resolve(DialogKeys.Required, "Требуется:") + "\n" + string.Join("\n", lines);
    }
}
