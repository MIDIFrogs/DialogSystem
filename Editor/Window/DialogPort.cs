using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    /// <summary>A port whose edge drops go to the dialog graph (so a drop on empty space creates a line).</summary>
    internal sealed class DialogPort : Port
    {
        private DialogPort(Direction direction, Capacity capacity)
            : base(Orientation.Horizontal, direction, capacity, typeof(bool))
        {
        }

        public PortRef Ref { get; private set; }

        public static DialogPort Create(Direction direction, Capacity capacity, IEdgeConnectorListener listener, PortRef reference)
        {
            var port = new DialogPort(direction, capacity) { Ref = reference, portName = string.Empty };
            port.m_EdgeConnector = new EdgeConnector<Edge>(listener);
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }
    }
}
