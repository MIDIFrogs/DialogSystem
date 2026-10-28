using System;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    [Serializable]
    public class AudioClipSound : IDialogSound
    {
        public AudioClip Clip;

        public float Length => Clip != null ? Clip.length : 0f;
    }
}
