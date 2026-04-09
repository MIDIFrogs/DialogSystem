using System;
using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Editor.Metadata;
using MIDIFrogs.DialogSystem.Unity;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Dialogs
{
    public class LineNode : Node
    {
        private const int MaxLength = 25;

        private DialogGraphView _graphView;
        private string text;
        private Label titleLabel;

        public DialogAuthorAsset Author { get; set; }
        public TMPro.FontStyles FontStyle { get; set; }
        public Sprite FrameSplash { get; set; }
        public LinesGroup Group { get; set; }
        public string GroupName => Group?.title ?? string.Empty;
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public List<DialogResponseData> Responses { get; set; } = new() { new() { Text = "Continue" } };

        public string Text
        {
            get => text;
            set
            {
                text = value;
                RefreshTitleText();
            }
        }

        public AudioClip Voice { get; set; }

        public void DisconnectAllPorts()
        {
            DisconnectInputPorts();
            DisconnectOutputPorts();
        }

        public void Initialize(DialogGraphView graphView, Vector2 position)
        {
            _graphView = graphView;
            SetPosition(new Rect(position, Vector2.zero));
            style.width = 300;

            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("ds-node__extension-container");

            // Title and input port
            titleLabel = (Label)titleContainer[0];
            RefreshTitleText();
            titleLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            titleLabel.style.fontSize = 14;

            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.AddManipulator(new EdgeConnector<Edge>(new GraphUpdateListener(this)));
            inputPort.portName = "";
            inputContainer.Add(inputPort);

            // Body foldout
            var bodyFold = new Foldout { text = "Line Details", value = true };
            var body = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            // Author field
            var authField = new ObjectField("Author") { objectType = typeof(DialogAuthorAsset), value = Author };
            authField.RegisterValueChangedCallback(evt => Author = (DialogAuthorAsset)evt.newValue);
            body.Add(authField);

            // Text field
            var textFoldout = new Foldout { text = "Message", value = true };
            var textArea = new TextField() { value = Text, multiline = true };
            textArea.RegisterValueChangedCallback(evt => Text = evt.newValue);
            textArea.style.minHeight = 60;
            textArea.AddToClassList("ds-node__text-field");
            textFoldout.Add(textArea);
            body.Add(textFoldout);

            // Responses
            var respFold = new Foldout { text = "Choices", value = true };
            var respContainer = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            var addRespButton = new Button(() =>
            {
                var newResp = new DialogResponseData { Text = "Choice", Conditions = new List<ConditionAsset>(), Actions = new List<ActionAsset>() };
                Responses.Add(newResp);
                respContainer.Add(CreateResponseFoldout(newResp));
                UpdateResponseRemoveButtons();
            })
            { text = "Add Choice" };
            respContainer.Add(addRespButton);

            foreach (var resp in Responses)
            {
                resp.Conditions ??= new List<ConditionAsset>();
                resp.Actions ??= new List<ActionAsset>();
                respContainer.Add(CreateResponseFoldout(resp));
            }
            respFold.Add(respContainer);
            body.Add(respFold);
            UpdateResponseRemoveButtons();

            // Extra properties
            var extraFold = new Foldout { text = "Extra Properties", value = false };
            var extraContainer = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            var voiceField = new ObjectField("Voice") { objectType = typeof(AudioClip), value = Voice };
            voiceField.RegisterValueChangedCallback(evt => Voice = (AudioClip)evt.newValue);
            extraContainer.Add(voiceField);

            var frameField = new ObjectField("FrameSplash") { objectType = typeof(Sprite), value = FrameSplash };
            frameField.RegisterValueChangedCallback(evt => FrameSplash = (Sprite)evt.newValue);
            extraContainer.Add(frameField);

            var fontField = new EnumField("Font Style", FontStyle);
            fontField.RegisterValueChangedCallback(evt => FontStyle = (TMPro.FontStyles)evt.newValue);
            extraContainer.Add(fontField);

            var groupField = new TextField("Group Name") { value = GroupName, isReadOnly = true };
            extraContainer.Add(groupField);

            extraFold.Add(extraContainer);
            body.Add(extraFold);

            bodyFold.Add(body);
            extensionContainer.Add(bodyFold);

            RefreshExpandedState();
        }

        public bool IsStartingNode()
        {
            var inputPort = inputContainer.Children().OfType<Port>().FirstOrDefault();
            return inputPort == null || !inputPort.connected;
        }

        public void ResetStyle()
        {
            mainContainer.style.backgroundColor = new Color(29 / 255f, 29 / 255f, 30 / 255f);
        }

        public void SetErrorStyle(Color color)
        {
            mainContainer.style.backgroundColor = color;
        }

        private VisualElement CreateResponseFoldout(DialogResponseData resp)
        {
            var foldStack = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart } };

            var fold = new Foldout { text = resp.Text, value = false };
            fold.style.flexGrow = 1;
            var container = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            // Port
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.userData = resp;
            port.portName = resp.Text;
            outputContainer.Add(port);

            // Text field
            var txt = new TextField() { value = resp.Text };
            txt.RegisterValueChangedCallback(evt =>
            {
                resp.Text = evt.newValue;
                fold.text = evt.newValue;
                var p = outputContainer.Children().OfType<Port>().FirstOrDefault(po => po.userData == resp);
                if (p != null) p.portName = resp.Text;
            });
            txt.AddToClassList("ds-node__text-field");
            container.Add(txt);

            // Conditions fold
            var condFold = new Foldout { text = "Conditions", value = false };
            var condList = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            var addCond = new Button(() =>
            {
                resp.Conditions ??= new List<ConditionAsset>();
                var condition = (ConditionAsset)null;
                resp.Conditions.Add(condition);

                condList.Add(CreateConditionRow(resp, condition));
            })
            { text = "Add Condition" };
            
            foreach (var condition in resp.Conditions)
            {
                condList.Add(CreateConditionRow(resp, condition));
            }

            condFold.Add(addCond);
            condFold.Add(condList);
            container.Add(condFold);

            // Actions fold
            var actFold = new Foldout { text = "Actions", value = false };
            var actList = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            var addAction = new Button(() =>
            {
                resp.Actions ??= new List<ActionAsset>();
                var action = (ActionAsset)null;
                resp.Actions.Add(action);

                actList.Add(CreateActionRow(resp, action));
            })
            { text = "Add Action" };

            foreach (var action in resp.Actions)
            {
                actList.Add(CreateActionRow(resp, action));
            }

            actFold.Add(addAction);
            actFold.Add(actList);
            container.Add(actFold);

            var removeBtn = new Button(() =>
            {
                if (Responses.Count <= 1)
                    return;

                Responses.Remove(resp);

                var port = outputContainer.Children()
                    .OfType<Port>()
                    .FirstOrDefault(p => p.userData == resp);

                if (port != null)
                {
                    if (port.connected)
                        _graphView.DeleteElements(port.connections);

                    _graphView.RemoveElement(port);
                }

                foldStack.RemoveFromHierarchy();

                RefreshPorts();
                UpdateResponseRemoveButtons();
            })
            {
                text = "X",
                tooltip = "Remove",
                userData = "remove_response"
            };
            removeBtn.style.marginTop = 4;
            removeBtn.SetEnabled(Responses.Count > 1);

            removeBtn.style.width = 20;
            foldStack.Add(removeBtn);
            fold.Add(container);
            foldStack.Add(fold);
            RefreshPorts();
            return foldStack;
        }

        private VisualElement CreateConditionRow(DialogResponseData resp, ConditionAsset condition)
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, maxWidth = 130 }
            };

            var of = new ObjectField
            {
                objectType = typeof(ConditionAsset),
                value = condition
            };

            of.RegisterValueChangedCallback(e =>
            {
                int index = resp.Conditions.IndexOf(condition);
                if (index >= 0)
                    resp.Conditions[index] = (ConditionAsset)e.newValue;

                condition = (ConditionAsset)e.newValue;
            });

            var removeBtn = new Button(() =>
            {
                resp.Conditions.Remove(condition);
                row.RemoveFromHierarchy();
            })
            {
                text = "X",
                tooltip = "Remove"
            };

            removeBtn.style.width = 20;

            row.Add(of);
            row.Add(removeBtn);

            return row;
        }

        private VisualElement CreateActionRow(DialogResponseData resp, ActionAsset action)
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, maxWidth = 130 }
            };

            var of = new ObjectField
            {
                objectType = typeof(ActionAsset),
                value = action
            };

            of.RegisterValueChangedCallback(e =>
            {
                int index = resp.Actions.IndexOf(action);
                if (index >= 0)
                    resp.Actions[index] = (ActionAsset)e.newValue;

                action = (ActionAsset)e.newValue;
            });

            var removeBtn = new Button(() =>
            {
                resp.Actions.Remove(action);
                row.RemoveFromHierarchy();
            })
            {
                text = "X",
                tooltip = "Remove"
            };

            removeBtn.style.width = 20;

            row.Add(of);
            row.Add(removeBtn);

            return row;
        }

        private void DisconnectInputPorts() => DisconnectPorts(inputContainer);

        private void DisconnectOutputPorts() => DisconnectPorts(outputContainer);

        private void DisconnectPorts(VisualElement container)
        {
            foreach (var port in container.Children().OfType<Port>().ToList())
            {
                if (port.connected)
                    _graphView.DeleteElements(port.connections);

                _graphView.RemoveElement(port);
            }
        }

        private void RefreshTitleText()
        {
            string titleText = text;
            if (string.IsNullOrWhiteSpace(titleText))
                titleText = "Dialog Line";
            if (titleText.Length > MaxLength)
                titleText = titleText[..MaxLength] + "...";

            if (titleLabel != null)
                titleLabel.text = titleText;
        }

        private void UpdateResponseRemoveButtons()
        {
            bool canRemove = Responses.Count > 1;

            foreach (var btn in extensionContainer.Query<Button>().Where(b => (string)b.userData == "remove_response").Build())
            {
                btn.SetEnabled(canRemove);
            }
        }

        private class GraphUpdateListener : IEdgeConnectorListener
        {
            private readonly LineNode _node;

            public GraphUpdateListener(LineNode node) => _node = node;

            public void OnDrop(GraphView graphView, Edge edge)
            {
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
            }
        }
    }
}