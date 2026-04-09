using MIDIFrogs.DialogSystem.Unity.View;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using TMPro;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    public class LineView : MonoBehaviour, ILineView
    {
        [SerializeField] private TMP_Text text;

        public UniTask ShowText(StyledText value, float? duration = null)
        {
            text.text = value.Content;
            text.fontStyle = (FontStyles)value.Style;
            text.color = value.DefaultColor;
            return UniTask.CompletedTask;
        }

        public void Skip() { }
    }
}
