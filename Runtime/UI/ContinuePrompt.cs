using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>
    /// The "Продолжить…" hint under a line without answers. Put it on a full-screen transparent Image so a click
    /// anywhere continues.
    /// </summary>
    public class ContinuePrompt : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string fallbackText = "Продолжить…";

        private bool clicked;

        /// <summary>Shows the prompt; without the label it only catches clicks (to finish typing).</summary>
        public void Show(IDialogTextResolver text, bool withLabel = true)
        {
            clicked = false;
            if (label != null)
            {
                label.text = (text ?? SourceTextResolver.Instance).Resolve(DialogKeys.Continue, fallbackText);
                label.enabled = withLabel;
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            clicked = false;
            gameObject.SetActive(false);
        }

        /// <summary>True once per click since the last call.</summary>
        public bool ConsumeClick()
        {
            bool result = clicked;
            clicked = false;
            return result;
        }

        public void OnPointerClick(PointerEventData eventData) => clicked = true;
    }
}
