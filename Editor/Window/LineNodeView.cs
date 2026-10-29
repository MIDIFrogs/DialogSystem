using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Editor.Model;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    /// <summary>
    /// A line: input and "Далее" in the author-coloured header, inline text, one row per answer with its badges
    /// and output, and "+ Ответ".
    /// </summary>
    internal sealed class LineNodeView : Node
    {
        private readonly DialogEditorWindow editor;
        private readonly Button authorButton;
        private readonly Label warningIcon;
        private readonly TextField text;
        private readonly Dictionary<string, AnswerRow> rows = new();

        public LineNodeView(DialogEditorWindow editor, DialogAsset.Line line, IEdgeConnectorListener listener)
        {
            this.editor = editor;
            LineId = line.Id;
            AddToClassList("dialog-node");
            topContainer.style.display = DisplayStyle.None;
            titleContainer.Clear();
            titleContainer.AddToClassList("dialog-node__header");

            Input = DialogPort.Create(Direction.Input, Port.Capacity.Multi, listener, PortRef.ForInput(line.Id));
            authorButton = new Button(ShowAuthorMenu);
            authorButton.AddToClassList("dialog-node__author");
            warningIcon = new Label("!");
            warningIcon.AddToClassList("dialog-warning-icon");
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            Next = DialogPort.Create(Direction.Output, Port.Capacity.Single, listener, PortRef.ForNext(line.Id));
            Next.portName = "Далее";
            titleContainer.Add(Input);
            titleContainer.Add(authorButton);
            titleContainer.Add(spacer);
            titleContainer.Add(warningIcon);
            titleContainer.Add(Next);

            text = new TextField { multiline = true };
            text.AddToClassList("dialog-node__text");
            text.textEdition.placeholder = "Текст реплики";
            text.RegisterValueChangedCallback(evt => editor.Change("Текст реплики", dialog =>
            {
                DialogAsset.Line target = dialog.FindLine(LineId);
                if (target != null)
                {
                    target.Text = evt.newValue;
                }
            }, ChangeKind.Typing, LineId + ".text"));
            extensionContainer.Add(text);

            foreach (DialogAsset.Answer answer in line.Answers)
            {
                var row = new AnswerRow(PortRef.ForAnswer(line.Id, answer.Id), listener);
                rows[answer.Id] = row;
                extensionContainer.Add(row.Root);
            }

            var add = new Button(() => editor.AddAnswer(LineId)) { text = "+ Ответ" };
            add.AddToClassList("dialog-node__add");
            extensionContainer.Add(add);

            Refresh(line);
            SetWarnings(new List<string>());
            RefreshExpandedState();
        }

        public string LineId { get; }

        public DialogPort Input { get; }

        public DialogPort Next { get; }

        public DialogPort AnswerPort(string answerId) => rows.TryGetValue(answerId, out AnswerRow row) ? row.Port : null;

        public void Refresh(DialogAsset.Line line)
        {
            authorButton.text = DialogEditorWindow.AuthorName(line.Author);
            Color color = line.Author != null ? line.Author.NameColor : new Color(0.45f, 0.45f, 0.45f);
            titleContainer.style.backgroundColor = Color.Lerp(color, Color.black, 0.55f);
            if (text.value != line.Text)
            {
                text.SetValueWithoutNotify(line.Text);
            }

            foreach (DialogAsset.Answer answer in line.Answers)
            {
                if (rows.TryGetValue(answer.Id, out AnswerRow row))
                {
                    row.Refresh(answer, editor);
                }
            }
        }

        public void SetWarnings(IReadOnlyList<string> messages)
        {
            warningIcon.style.display = messages.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            warningIcon.tooltip = string.Join("\n", messages);
            EnableInClassList("dialog-node--warning", messages.Count > 0);
        }

        private void ShowAuthorMenu()
        {
            DialogAsset.Line line = editor.Dialog != null ? editor.Dialog.FindLine(LineId) : null;
            if (line == null)
            {
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Без автора"), line.Author == null, () => SetAuthor(null));
            foreach (DialogAuthorAsset author in editor.Authors)
            {
                DialogAuthorAsset item = author;
                menu.AddItem(new GUIContent(DialogEditorWindow.AuthorName(item)), line.Author == item, () => SetAuthor(item));
            }
            menu.DropDown(authorButton.worldBound);
        }

        private void SetAuthor(DialogAuthorAsset author) =>
            editor.Change("Автор реплики", dialog =>
            {
                DialogAsset.Line target = dialog.FindLine(LineId);
                if (target != null)
                {
                    target.Author = author;
                }
            }, ChangeKind.Value);

        private sealed class AnswerRow
        {
            private readonly Label text;
            private readonly Label visible;
            private readonly Label locked;
            private readonly Label actions;

            public AnswerRow(PortRef reference, IEdgeConnectorListener listener)
            {
                Root = new VisualElement();
                Root.AddToClassList("dialog-answer");
                text = new Label();
                text.AddToClassList("dialog-answer__text");
                visible = Badge("dialog-badge--visible");
                locked = Badge("dialog-badge--locked");
                actions = Badge("dialog-badge--actions");
                Port = DialogPort.Create(Direction.Output, UnityEditor.Experimental.GraphView.Port.Capacity.Single, listener, reference);
                Root.Add(text);
                Root.Add(visible);
                Root.Add(locked);
                Root.Add(actions);
                Root.Add(Port);
            }

            public VisualElement Root { get; }

            public DialogPort Port { get; }

            public void Refresh(DialogAsset.Answer answer, DialogEditorWindow editor)
            {
                text.text = string.IsNullOrEmpty(answer.Text) ? "(пустой ответ)" : answer.Text;
                SetBadge(visible, "вид", ExpressionBadges.Clauses(answer.VisibleIf, editor.Functions), "Видимость: " + answer.VisibleIf);
                SetBadge(locked, "доступ", ExpressionBadges.Clauses(answer.EnabledIf, editor.Functions), "Доступность: " + answer.EnabledIf);
                SetBadge(actions, "действ.", ExpressionBadges.Actions(answer.Actions, editor.Functions), "Действия: " + answer.Actions);
            }

            private static Label Badge(string kind)
            {
                var badge = new Label();
                badge.AddToClassList("dialog-badge");
                badge.AddToClassList(kind);
                return badge;
            }

            private static void SetBadge(Label badge, string icon, int count, string tooltip)
            {
                badge.style.display = count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                badge.text = count < 0 ? icon + " !" : $"{icon} {count}";
                badge.tooltip = tooltip;
                badge.EnableInClassList("dialog-badge--error", count < 0);
            }
        }
    }
}
