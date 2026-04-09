using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.View
{
    public interface IVoicePlayer
    {
        void Play(IDialogVoiceover voice);
        void Stop();
    }
}
