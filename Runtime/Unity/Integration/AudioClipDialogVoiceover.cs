using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Integration
{
    public class AudioClipDialogVoiceover : IDialogVoiceover
    {
        private readonly AudioClip clip;

        public AudioClipDialogVoiceover(AudioClip clip)
        {
            this.clip = clip;
        }

        public float Duration => clip != null ? clip.length : 0;

        public void Play(IVoiceoverContext context)
        {
            if (clip != null && context.TryGetFeature<AudioSource>(out var source))
            {
                source.clip = clip;
                source.Play();
            }
        }
    }
}
