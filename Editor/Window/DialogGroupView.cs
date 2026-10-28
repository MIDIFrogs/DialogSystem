using UnityEditor.Experimental.GraphView;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    internal sealed class DialogGroupView : Group
    {
        public DialogGroupView(string groupId, string groupTitle)
        {
            GroupId = groupId;
            title = groupTitle;
            AddToClassList("dialog-group");
        }

        public string GroupId { get; }
    }
}
