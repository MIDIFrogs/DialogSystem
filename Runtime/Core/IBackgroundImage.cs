namespace MIDIFrogs.DialogSystem.Core
{
    public interface IBackgroundImage
    {
        bool TryGetImage<T>(out T image) where T : class;
    }
}
