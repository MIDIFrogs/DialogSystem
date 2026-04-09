using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Unity.View;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    public class ChoicesView : MonoBehaviour, IChoicesView
    {
        [SerializeField] private Transform container;
        [SerializeField] private ResponseButton prefab;

        public async UniTask<DialogChoice> ShowChoices(IEnumerable<DialogChoice> choices)
        {
            Clear();

            var tasks = new List<UniTask<DialogChoice>>();

            foreach (var choice in choices)
            {
                var instance = Instantiate(prefab, container);
                tasks.Add(instance.Bind(choice));
            }

            var result = await UniTask.WhenAny(tasks);
            return result.result;
        }

        public void Clear()
        {
            foreach (Transform child in container)
                Destroy(child.gameObject);
        }
    }
}
