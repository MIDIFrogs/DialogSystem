using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    /// <summary>The Start node: one output, cannot be deleted.</summary>
    internal sealed class StartNodeView : Node
    {
        private readonly Label warningIcon;

        public StartNodeView(IEdgeConnectorListener listener)
        {
            title = "Старт";
            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable | Capabilities.Collapsible | Capabilities.Renamable);
            AddToClassList("dialog-start");
            Output = DialogPort.Create(Direction.Output, Port.Capacity.Single, listener, PortRef.ForStart());
            outputContainer.Add(Output);
            warningIcon = new Label("!");
            warningIcon.AddToClassList("dialog-warning-icon");
            titleContainer.Add(warningIcon);
            SetWarnings(new List<string>());
            RefreshPorts();
            RefreshExpandedState();
        }

        public DialogPort Output { get; }

        public void SetWarnings(IReadOnlyList<string> messages)
        {
            warningIcon.style.display = messages.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            warningIcon.tooltip = string.Join("\n", messages);
            EnableInClassList("dialog-node--warning", messages.Count > 0);
        }
    }
}
