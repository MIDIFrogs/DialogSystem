using MIDIFrogs.DialogSystem.Unity.Data;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    /// <summary>Plays voice lines and typing blips. Sound kinds a player does not know are ignored.</summary>
    public interface IDialogSoundPlayer
    {
        void PlayVoice(IDialogSound voice);

        void StopVoice();

        void Blip(IDialogSound sound, float pitchVariance);
    }
}
