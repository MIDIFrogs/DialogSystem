namespace MIDIFrogs.DialogSystem.Editor.Settings
{
    /// <summary>A localization key with its text in the source language.</summary>
    public readonly struct DialogString
    {
        public DialogString(string key, string sourceText)
        {
            Key = key;
            SourceText = sourceText;
        }

        public string Key { get; }

        public string SourceText { get; }
    }
}
