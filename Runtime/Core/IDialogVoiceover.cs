namespace MIDIFrogs.DialogSystem.Core
{
    public interface IDialogVoiceover
    {
        float Duration { get; }

        void Play(IVoiceoverContext context);
    }

    public interface IVoiceoverContext
    {
        bool TryGetFeature<T>(out T feature) where T : class;
    }
}
