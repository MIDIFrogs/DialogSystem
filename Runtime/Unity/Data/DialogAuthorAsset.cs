using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    [CreateAssetMenu(menuName = "Dialog System/Author")]
    public class DialogAuthorAsset : ScriptableObject
    {
        public string Id;
        public string DisplayName;

        public Sprite Avatar;
        public Color NameColor;
    }
}