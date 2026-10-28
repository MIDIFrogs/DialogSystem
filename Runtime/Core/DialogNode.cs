using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core
{
    public class DialogNode
    {
        public string Id;
        public StyledText Text = new();
        public DialogAuthor Author;

        /// <summary>Engine-side voice (e.g. an IDialogSound); null for none.</summary>
        public object Voice;
        public IBackgroundImage BackgroundImage;

        /// <summary>Where the dialog goes when no answer is visible and enabled; null ends it.</summary>
        public DialogNode Next;
        public List<DialogChoice> Choices = new();
    }
}
