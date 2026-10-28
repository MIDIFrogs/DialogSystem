using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.UI
{
    /// <summary>Plays <see cref="AudioClipSound"/> voices and blips through two AudioSources (created if unset).</summary>
    public class DialogSoundPlayer : MonoBehaviour, IDialogSoundPlayer
    {
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource blipSource;

        public AudioSource VoiceSource => voiceSource;

        public AudioSource BlipSource => blipSource;

        public bool IsVoicePlaying { get; private set; }

        public void PlayVoice(IDialogSound voice)
        {
            StopVoice();
            if (voice is not AudioClipSound { Clip: not null } clip)
            {
                return;
            }

            voiceSource.clip = clip.Clip;
            voiceSource.Play();
            IsVoicePlaying = true;
        }

        public void StopVoice()
        {
            if (voiceSource != null)
            {
                voiceSource.Stop();
            }
            IsVoicePlaying = false;
        }

        public void Blip(IDialogSound sound, float pitchVariance)
        {
            if (sound is not AudioClipSound { Clip: not null } clip)
            {
                return;
            }

            blipSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            blipSource.PlayOneShot(clip.Clip);
        }

        private void Awake()
        {
            voiceSource ??= CreateSource("Voice");
            blipSource ??= CreateSource("Blips");
        }

        private void Update()
        {
            // The voice finished on its own.
            if (IsVoicePlaying && voiceSource != null && !voiceSource.isPlaying)
            {
                IsVoicePlaying = false;
            }
        }

        private AudioSource CreateSource(string sourceName)
        {
            var source = new GameObject(sourceName).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            return source;
        }
    }
}
