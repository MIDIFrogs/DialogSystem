using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MIDIFrogs.DialogSystem.UI
{
    public class ChoicesView : MonoBehaviour, IChoicesView
    {
        [SerializeField] private Transform container;
        [SerializeField] private ResponseButton prefab;

        private readonly List<ResponseButton> buttons = new();

        public async UniTask<DialogChoice> ShowChoices(IReadOnlyList<ChoiceState> choices, IDialogTextResolver text,
            CancellationToken token)
        {
            Clear();
            var picks = new List<UniTask<DialogChoice>>();
            ResponseButton first = null;
            foreach (ChoiceState state in choices)
            {
                ResponseButton button = Instantiate(prefab, container);
                button.gameObject.SetActive(true);
                buttons.Add(button);
                UniTask<DialogChoice> pick = button.Bind(state, text);
                if (state.Enabled)
                {
                    picks.Add(pick);
                    first ??= button;
                }
            }

            if (first != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }

            if (picks.Count == 0)
            {
                // Locked answers only: shown for their hints; the line continues through the root view.
                await UniTask.Never(token);
            }

            (int _, DialogChoice choice) = await UniTask.WhenAny(picks).AttachExternalCancellation(token);
            return choice;
        }

        public void Clear()
        {
            foreach (ResponseButton button in buttons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            buttons.Clear();
        }
    }
}
