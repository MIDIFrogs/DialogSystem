using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    [CreateAssetMenu(menuName = "Dialog System/Author")]
    public class DialogAuthorAsset : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Sprite Avatar;
        public Color NameColor = Color.white;

        [Tooltip("Played while this author's text types out; empty uses the dialog view's default.")]
        [SerializeReference, DialogSound] public IDialogSound TypingSound;

        [Min(1)] public int BlipEvery = 2;
        [Range(0f, 0.5f)] public float PitchVariance = 0.05f;
    }
}
