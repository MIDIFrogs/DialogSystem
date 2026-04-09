using MIDIFrogs.DialogSystem.Unity.View;
using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    public class VoicePlayer : MonoBehaviour, IVoicePlayer
    {
        [SerializeField] private AudioSource audioSource;

        private VoiceoverContext context;

        private void Awake()
        {
            context = new VoiceoverContext(audioSource);
        }

        public void Play(IDialogVoiceover voice)
        {
            if (audioSource == null)
                return;

            audioSource.Stop();
            voice?.Play(context);
        }

        public void Stop()
        {
            if (audioSource != null)
                audioSource.Stop();
        }

        private class VoiceoverContext : IVoiceoverContext
        {
            private readonly AudioSource source;

            public VoiceoverContext(AudioSource source)
            {
                this.source = source;
            }

            public bool TryGetFeature<T>(out T feature) where T : class
            {
                feature = source as T;
                return feature != null;
            }
        }
    }
}
