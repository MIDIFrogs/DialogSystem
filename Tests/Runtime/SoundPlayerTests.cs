using MIDIFrogs.DialogSystem.UI;
using MIDIFrogs.DialogSystem.Unity.Data;
using NUnit.Framework;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class SoundPlayerTests
    {
        private sealed class OtherSound : IDialogSound
        {
            public float Length => 1f;
        }

        private GameObject host;
        private DialogSoundPlayer player;
        private AudioClip clip;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Sound");
            player = host.AddComponent<DialogSoundPlayer>();
            clip = AudioClip.Create("beep", 4800, 1, 48000, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(host);
            Object.Destroy(clip);
        }

        [Test]
        public void VoicePlaysAndStops()
        {
            player.PlayVoice(new AudioClipSound { Clip = clip });
            Assert.AreSame(clip, player.VoiceSource.clip);
            Assert.IsTrue(player.IsVoicePlaying);

            player.StopVoice();
            Assert.IsFalse(player.IsVoicePlaying);
        }

        [Test]
        public void BlipPitchStaysInRange()
        {
            var blip = new AudioClipSound { Clip = clip };
            for (int i = 0; i < 100; i++)
            {
                player.Blip(blip, 0.05f);
                Assert.That(player.BlipSource.pitch, Is.InRange(0.95f, 1.05f));
            }
        }

        [Test]
        public void UnknownSoundTypeIsIgnored()
        {
            Assert.DoesNotThrow(() => player.PlayVoice(new OtherSound()));
            Assert.DoesNotThrow(() => player.Blip(new OtherSound(), 0.05f));
            Assert.IsFalse(player.IsVoicePlaying);
            Assert.DoesNotThrow(() => player.PlayVoice(null));
        }
    }
}
