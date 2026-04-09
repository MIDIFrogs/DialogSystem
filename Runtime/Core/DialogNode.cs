using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core
{
    public class DialogNode
    {
        public StyledText Text;
        public DialogAuthor Author;
        public IDialogVoiceover Voice;
        public IBackgroundImage BackgroundImage;
        public List<DialogChoice> Choices = new();
    }
}
