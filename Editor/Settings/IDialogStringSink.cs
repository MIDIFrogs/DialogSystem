using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Editor.Settings
{
    /// <summary>
    /// Receives the dialog strings in the source language, e.g. to write them into the game's string table.
    /// Implement it on a ScriptableObject and assign that object in <see cref="DialogEditorSettings"/>.
    /// </summary>
    public interface IDialogStringSink
    {
        void Write(IReadOnlyList<DialogString> strings, string sourceLanguage);
    }
}
