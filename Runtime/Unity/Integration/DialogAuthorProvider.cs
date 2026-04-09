using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEngine;

namespace Assets.MIDIFrogs.DialogSystem.Runtime.Unity.Integration
{
    [CreateAssetMenu(menuName = "Dialog System/Author Provider")]
    public class DialogAuthorProvider : ScriptableObject, IDialogAuthorProvider
    {
        [SerializeField] private List<DialogAuthorAsset> authors = new();

        private Dictionary<string, DialogAuthorAsset> map;

        private void Awake() => Rebuild();

        private void Rebuild()
        {
            map = new Dictionary<string, DialogAuthorAsset>();

            authors ??= new();
            foreach (var a in authors.Where(x => x))
                map[a.Id] = a;
        }

        private void OnValidate() => Rebuild();

        public Sprite GetAvatar(string id) => map[id].Avatar;

        public Color GetColor(string id) => map[id].NameColor;

        public string GetName(string id) => map[id].DisplayName;
    }
}