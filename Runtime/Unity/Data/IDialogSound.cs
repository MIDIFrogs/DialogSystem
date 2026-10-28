namespace MIDIFrogs.DialogSystem.Unity.Data
{
    /// <summary>
    /// A voice line or typing sound, engine-neutral: the package plays <see cref="AudioClipSound"/>; games add
    /// their own kinds (e.g. an FMOD event) with their own player.
    /// </summary>
    public interface IDialogSound
    {
        /// <summary>Length in seconds, 0 if unknown.</summary>
        float Length { get; }
    }
}
