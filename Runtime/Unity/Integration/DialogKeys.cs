namespace MIDIFrogs.DialogSystem.Unity.Integration
{
    /// <summary>Localization keys of dialog texts. They use the dialog's stable Id, not its file name.</summary>
    public static class DialogKeys
    {
        public const string Continue = "dialog.continue";

        /// <summary>The heading of a locked answer's hint ("Требуется:").</summary>
        public const string Required = "dialog.required";

        public static string Line(string dialogId, string lineId) => $"dialog.{dialogId}.{lineId}";

        public static string Answer(string dialogId, string lineId, string answerId) => $"dialog.{dialogId}.{lineId}.{answerId}";

        public static string Hint(string dialogId, string lineId, string answerId) => $"dialog.{dialogId}.{lineId}.{answerId}.hint";

        public static string Author(string authorId) => $"dialog.author.{authorId}";
    }
}
