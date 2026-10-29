namespace MIDIFrogs.DialogSystem.Core
{
    /// <summary>The default resolver: shows the authored text as is.</summary>
    public sealed class SourceTextResolver : IDialogTextResolver
    {
        public static readonly SourceTextResolver Instance = new();

        public string Resolve(string key, string sourceText) => sourceText;
    }
}
