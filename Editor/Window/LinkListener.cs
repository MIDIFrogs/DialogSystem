using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    internal sealed class LinkListener : IEdgeConnectorListener
    {
        private readonly DialogGraphView graph;

        public LinkListener(DialogGraphView graph)
        {
            this.graph = graph;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            if (edge.output is DialogPort from)
            {
                graph.CreateLinkedLine(from.Ref, position);
            }
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            if (edge.output is DialogPort from && edge.input is DialogPort to)
            {
                graph.Link(from.Ref, to.Ref.LineId);
            }
        }
    }
}
