using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core
{
    public class DialogChoice
    {
        public string Text;
        public List<IDialogCondition> Conditions = new();
        public List<IDialogAction> Actions = new();
        public DialogNode NextNode;
    }
}
