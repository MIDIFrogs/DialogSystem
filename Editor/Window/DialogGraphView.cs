using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Editor.Model;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    /// <summary>
    /// The graph of one dialog. It is a view of the asset: edits go to the asset through the window, and the
    /// graph is rebuilt from the asset after structural changes and Undo.
    /// </summary>
    internal sealed class DialogGraphView : GraphView
    {
        private readonly DialogEditorWindow editor;
        private readonly LinkListener listener;
        private readonly MiniMap miniMap;
        private readonly Dictionary<string, LineNodeView> lines = new();
        private StartNodeView start;
        private bool loading;
        private bool selectionScheduled;

        public DialogGraphView(DialogEditorWindow editor)
        {
            this.editor = editor;
            listener = new LinkListener(this);
            AddToClassList("dialog-graph");
            style.flexGrow = 1;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 10, 180, 120));
            Add(miniMap);

            graphViewChanged = OnGraphViewChanged;
            groupTitleChanged = OnGroupTitleChanged;
            elementsAddedToGroup = (group, elements) => OnGroupMembership(group, elements, true);
            elementsRemovedFromGroup = (group, elements) => OnGroupMembership(group, elements, false);
        }

        public void SetMiniMapVisible(bool visible) => miniMap.visible = visible;

        public void Load(DialogAsset dialog)
        {
            loading = true;
            try
            {
                var selected = new HashSet<string>(selection.OfType<LineNodeView>().Select(node => node.LineId));
                DeleteElements(graphElements.ToList());
                lines.Clear();
                start = null;
                if (dialog == null)
                {
                    return;
                }

                start = new StartNodeView(listener);
                start.SetPosition(new Rect(dialog.StartPosition, Vector2.zero));
                AddElement(start);

                foreach (DialogAsset.Line line in dialog.Lines)
                {
                    var view = new LineNodeView(editor, line, listener);
                    view.SetPosition(new Rect(line.Position, Vector2.zero));
                    AddElement(view);
                    lines[line.Id] = view;
                }

                foreach (DialogAsset.Group group in dialog.Groups)
                {
                    var view = new DialogGroupView(group.Id, group.Title);
                    view.SetPosition(new Rect(group.Position, Vector2.zero));
                    AddElement(view);
                    view.AddElements(lines.Values.Where(node => dialog.FindLine(node.LineId).GroupId == group.Id));
                }

                if (lines.TryGetValue(dialog.StartNodeId ?? string.Empty, out LineNodeView first))
                {
                    Connect(start.Output, first.Input);
                }

                foreach (DialogAsset.Line line in dialog.Lines)
                {
                    LineNodeView from = lines[line.Id];
                    if (lines.TryGetValue(line.NextId ?? string.Empty, out LineNodeView next))
                    {
                        Connect(from.Next, next.Input);
                    }

                    foreach (DialogAsset.Answer answer in line.Answers)
                    {
                        if (lines.TryGetValue(answer.TargetId ?? string.Empty, out LineNodeView target))
                        {
                            Connect(from.AnswerPort(answer.Id), target.Input);
                        }
                    }
                }

                foreach (string id in selected)
                {
                    if (lines.TryGetValue(id, out LineNodeView view))
                    {
                        AddToSelection(view);
                    }
                }
            }
            finally
            {
                loading = false;
            }
        }

        /// <summary>Updates texts, authors and badges in place, without rebuilding.</summary>
        public void RefreshNodes(DialogAsset dialog)
        {
            foreach (LineNodeView view in lines.Values)
            {
                DialogAsset.Line line = dialog.FindLine(view.LineId);
                if (line != null)
                {
                    view.Refresh(line);
                }
            }
        }

        public void ShowWarnings(IReadOnlyList<DialogWarning> warnings)
        {
            start?.SetWarnings(warnings.Where(w => w.LineId == null && w.Field == nameof(DialogAsset.StartNodeId))
                .Select(w => w.Message).ToList());
            foreach (LineNodeView view in lines.Values)
            {
                view.SetWarnings(warnings.Where(w => w.LineId == view.LineId).Select(w => w.Message).ToList());
            }
        }

        public void SelectLine(string lineId)
        {
            if (lines.TryGetValue(lineId ?? string.Empty, out LineNodeView view))
            {
                ClearSelection();
                AddToSelection(view);
                FrameSelection();
            }
        }

        public void Link(PortRef from, string toLineId)
        {
            DialogAsset dialog = editor.Dialog;
            if (dialog == null)
            {
                return;
            }

            switch (from.Kind)
            {
                case PortKind.Start:
                    DialogGraphEditing.SetStart(dialog, toLineId);
                    break;
                case PortKind.Next:
                    DialogGraphEditing.SetNext(dialog, from.LineId, toLineId);
                    break;
                case PortKind.Answer:
                    DialogGraphEditing.SetAnswerTarget(dialog, from.LineId, from.AnswerId, toLineId);
                    break;
            }
            editor.AfterChange(ChangeKind.Structure);
        }

        /// <summary>A link dropped on empty space: a new empty line there, with the palette author, linked.</summary>
        public void CreateLinkedLine(PortRef from, Vector2 worldPosition)
        {
            DialogAsset dialog = editor.Dialog;
            if (dialog == null)
            {
                return;
            }

            Vector2 position = contentViewContainer.WorldToLocal(worldPosition);
            DialogAsset.Line line = DialogGraphEditing.AddLine(dialog, position, editor.SelectedAuthor);
            editor.SelectLineAfterReload(line.Id);
            Link(from, line.Id);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) =>
            ports.Where(port => port.direction != startPort.direction && port.node != startPort.node).ToList();

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (editor.Dialog != null && evt.target is GraphView)
            {
                Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
                evt.menu.AppendAction("Добавить реплику", _ =>
                {
                    DialogAsset.Line line = DialogGraphEditing.AddLine(editor.Dialog, position, editor.SelectedAuthor);
                    editor.SelectLineAfterReload(line.Id);
                    editor.AfterChange(ChangeKind.Structure);
                });
            }

            List<string> selectedLines = selection.OfType<LineNodeView>().Select(node => node.LineId).ToList();
            if (editor.Dialog != null && selectedLines.Count > 0)
            {
                evt.menu.AppendAction("Сгруппировать выделенное", _ =>
                {
                    Vector2 position = selection.OfType<LineNodeView>().First().GetPosition().position - new Vector2(20, 40);
                    DialogAsset.Group group = DialogGraphEditing.AddGroup(editor.Dialog, "Группа", position);
                    DialogGraphEditing.SetGroup(editor.Dialog, selectedLines, group.Id);
                    editor.AfterChange(ChangeKind.Structure);
                });
            }

            evt.menu.AppendSeparator();
            base.BuildContextualMenu(evt);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            ScheduleSelectionChanged();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            ScheduleSelectionChanged();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            ScheduleSelectionChanged();
        }

        private void ScheduleSelectionChanged()
        {
            if (selectionScheduled)
            {
                return;
            }

            selectionScheduled = true;
            schedule.Execute(() =>
            {
                selectionScheduled = false;
                List<LineNodeView> selected = selection.OfType<LineNodeView>().ToList();
                editor.OnLineSelected(selected.Count == 1 ? selected[0].LineId : null);
            });
        }

        private void Connect(Port output, Port input)
        {
            if (output == null || input == null)
            {
                return;
            }

            AddElement(output.ConnectTo(input));
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            DialogAsset dialog = editor.Dialog;
            if (loading || dialog == null)
            {
                return change;
            }

            bool structure = false;
            bool moved = false;
            if (change.elementsToRemove != null)
            {
                var removedLines = new List<string>();
                foreach (GraphElement element in change.elementsToRemove)
                {
                    switch (element)
                    {
                        case Edge { output: DialogPort port }:
                            Unlink(dialog, port.Ref);
                            structure = true;
                            break;
                        case LineNodeView node:
                            removedLines.Add(node.LineId);
                            break;
                        case DialogGroupView group:
                            DialogGraphEditing.RemoveGroup(dialog, group.GroupId);
                            structure = true;
                            break;
                    }
                }

                if (removedLines.Count > 0)
                {
                    DialogGraphEditing.RemoveLines(dialog, removedLines);
                    structure = true;
                }
            }

            if (change.movedElements != null)
            {
                foreach (GraphElement element in change.movedElements)
                {
                    Vector2 position = element.GetPosition().position;
                    switch (element)
                    {
                        case StartNodeView:
                            DialogGraphEditing.Record(dialog, "Move Start");
                            dialog.StartPosition = position;
                            moved = true;
                            break;
                        case LineNodeView node:
                            DialogGraphEditing.MoveLine(dialog, node.LineId, position);
                            moved = true;
                            break;
                        case DialogGroupView group:
                            DialogAsset.Group data = dialog.Groups.Find(g => g.Id == group.GroupId);
                            if (data != null)
                            {
                                DialogGraphEditing.Record(dialog, "Move group");
                                data.Position = position;
                                moved = true;
                            }
                            break;
                    }
                }
            }

            if (structure)
            {
                editor.AfterChange(ChangeKind.Structure);
            }
            else if (moved)
            {
                editor.AfterChange(ChangeKind.Value);
            }
            return change;
        }

        private static void Unlink(DialogAsset dialog, PortRef from)
        {
            switch (from.Kind)
            {
                case PortKind.Start:
                    DialogGraphEditing.SetStart(dialog, null);
                    break;
                case PortKind.Next:
                    DialogGraphEditing.SetNext(dialog, from.LineId, null);
                    break;
                case PortKind.Answer:
                    DialogGraphEditing.SetAnswerTarget(dialog, from.LineId, from.AnswerId, null);
                    break;
            }
        }

        private void OnGroupTitleChanged(Group group, string title)
        {
            if (loading || editor.Dialog == null || group is not DialogGroupView view)
            {
                return;
            }

            editor.Change("Название группы", dialog =>
            {
                DialogAsset.Group data = dialog.Groups.Find(g => g.Id == view.GroupId);
                if (data != null)
                {
                    data.Title = title;
                }
            }, ChangeKind.Value);
        }

        private void OnGroupMembership(Group group, IEnumerable<GraphElement> elements, bool added)
        {
            if (loading || editor.Dialog == null || group is not DialogGroupView view)
            {
                return;
            }

            List<string> ids = elements.OfType<LineNodeView>().Select(node => node.LineId).ToList();
            if (ids.Count == 0)
            {
                return;
            }

            DialogGraphEditing.SetGroup(editor.Dialog, ids, added ? view.GroupId : null);
            editor.AfterChange(ChangeKind.Value);
        }
    }
}
