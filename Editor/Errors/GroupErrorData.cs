using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Editor.Dialogs;

namespace MIDIFrogs.DialogSystem.Editor.Errors
{
    public class GroupErrorData
    {
        public DialogErrorData ErrorData { get; set; }
        public List<LinesGroup> Groups { get; set; }

        public GroupErrorData()
        {
            ErrorData = new DialogErrorData();
            Groups = new List<LinesGroup>();
        }
    }
}