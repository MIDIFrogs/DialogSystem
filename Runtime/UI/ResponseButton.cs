using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MIDIFrogs.DialogSystem.UI
{
    public class ResponseButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Button button;

        private UniTaskCompletionSource<DialogChoice> tcs;

        public UniTask<DialogChoice> Bind(DialogChoice choice)
        {
            text.text = choice.Text;

            tcs = new UniTaskCompletionSource<DialogChoice>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => tcs.TrySetResult(choice));

            return tcs.Task;
        }
    }
}