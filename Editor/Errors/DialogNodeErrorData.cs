using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Editor.Dialogs;

namespace MIDIFrogs.DialogSystem.Editor.Errors
{
    public class DialogNodeErrorData
    {
        public DialogErrorData ErrorData { get; set; }
        public List<LineNode> Nodes { get; set; }

        public DialogNodeErrorData()
        {
            ErrorData = new DialogErrorData();
            Nodes = new List<LineNode>();
        }
    }
}