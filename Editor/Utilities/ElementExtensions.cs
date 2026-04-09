using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Utilities
{
    public static class ElementExtensions
    {
        public static Button CreateButton(this VisualElement element, string text, Action onClick = null)
        {
            Button button = new(onClick)
            {
                text = text
            };

            element?.Add(button);
            return button;
        }

        public static Foldout CreateFoldout(this VisualElement element, string title, bool collapsed = false)
        {
            Foldout foldout = new()
            {
                text = title,
                value = !collapsed
            };

            element?.Add(foldout);
            return foldout;
        }

        public static Port CreatePort(this Node node, string portName = "", Orientation orientation = Orientation.Horizontal, Direction direction = Direction.Output, Port.Capacity capacity = Port.Capacity.Single)
        {
            Port port = node.InstantiatePort(orientation, direction, capacity, typeof(bool));

            port.portName = portName;

            return port;
        }

        public static TextField CreateTextField(this VisualElement element, string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textField = new()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            element?.Add(textField);
            return textField;
        }

        public static TextField CreateTextArea(this VisualElement element, string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textArea = CreateTextField(element, value, label, onValueChanged);

            textArea.multiline = true;

            return textArea;
        }
    }
}