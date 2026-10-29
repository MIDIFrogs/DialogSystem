namespace MIDIFrogs.DialogSystem.Core
{
    /// <summary>Turns a localization key and the authored text into the text shown to the player.</summary>
    public interface IDialogTextResolver
    {
        string Resolve(string key, string sourceText);
    }
}
