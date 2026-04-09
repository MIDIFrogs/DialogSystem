using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MIDIFrogs.DialogSystem.Editor.Errors;
using MIDIFrogs.DialogSystem.Editor.Metadata;
using MIDIFrogs.DialogSystem.Editor.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Dialogs
{
    public class DialogGraphView : GraphView
    {
        private readonly DialogEditorWindow editorWindow;
        private readonly SerializableDictionary<string, GroupErrorData> groups = new();
        private readonly DialogNodeErrorData nodeErrors = new();
        private MiniMap miniMap;
        private Button miniMapButton;

        // Error-tracking moved here
        private int nameErrorsCount;

        private int rootNodesCount;

        // Save button is now created in toolbar
        private Button saveButton;

        private DialogSearchWindow searchWindow;

        public DialogGraphView(DialogEditorWindow editorWindow)
        {
            this.editorWindow = editorWindow;
            style.flexGrow = 1;

            AddManipulators();
            AddGridBackground();
            AddSearchWindow();
            AddMiniMap();
            schedule.Execute(UpdateMiniMapPosition).Every(100);
            AddToolbar();

            AddStyles();
            AddMiniMapStyles();

            SetupOnElementsDeleted();
            SetupOnGroupRenamed();
            SetupOnGraphViewChanged();
        }

        public event Action GraphChanged;

        public int NameErrorsCount
        {
            get => nameErrorsCount;
            set
            {
                nameErrorsCount = value;
                if (value > 0)
                {
                    saveButton.SetEnabled(false);
                }
                else
                {
                    saveButton.SetEnabled(true);
                }
            }
        }

        public int RootNodesCount
        {
            get => rootNodesCount;
            set
            {
                rootNodesCount = value;
                if (rootNodesCount > 1)
                {
                    saveButton.SetEnabled(false);
                }
                else
                {
                    saveButton.SetEnabled(true);
                }
            }
        }

        public void ClearGraph()
        {
            graphElements.ToList().ForEach(RemoveElement);
            groups.Clear();
            NameErrorsCount = 0;
            OnGraphChanged();
        }

        public LinesGroup CreateGroup(string title, Vector2 position)
        {
            LinesGroup group = new(title, position);

            AddGroup(group);

            AddElement(group);

            foreach (GraphElement selectedElement in selection.Cast<GraphElement>())
            {
                if (selectedElement is not LineNode)
                {
                    continue;
                }

                LineNode node = (LineNode)selectedElement;

                group.AddElement(node);
            }

            return group;
        }

        public LinesGroup CreateGroupAt(string title, Vector2 screenPos)
        {
            var local = GetLocalMousePosition(screenPos, true);
            var grp = CreateGroup(title, local);
            AddElement(grp);
            OnGraphChanged();
            return grp;
        }

        public LineNode CreateNode(Vector2 position, bool suppressInitialize = false)
        {
            var node = new LineNode
            {
                Author = editorWindow.selectedAuthor
            };
            if (!suppressInitialize)
                node.Initialize(this, position);

            return node;
        }

        // === Node & Group Creation Helpers ===
        public LineNode CreateNodeAt(Vector2 screenPos, bool suppressInitialize = false)
        {
            return CreateAndRecordNode(screenPos, suppressInitialize);
        }

        // === Core Overrides ===
        public override List<Port> GetCompatiblePorts(Port start, NodeAdapter na)
        {
            return ports.Where(p => p != start && p.node != start.node && p.direction != start.direction).ToList();
        }

        // === Utilities ===
        public Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false)
        {
            Vector2 worldMousePosition = mousePosition;

            if (isSearchWindow)
            {
                worldMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(editorWindow.rootVisualElement.parent, mousePosition - editorWindow.position.position);
            }

            Vector2 localMousePosition = contentViewContainer.WorldToLocal(worldMousePosition);

            return localMousePosition;
        }

        public void OnGraphChanged()
        {
            ValidateRootNodes();
            GraphChanged?.Invoke();
        }

        public void ToggleMiniMap()
        {
            miniMap.visible = !miniMap.visible;
            miniMapButton.ToggleInClassList("ds-toolbar__button__selected");
        }

        // === UI Setup ===
        private void AddGridBackground()
        {
            var bg = new GridBackground(); bg.StretchToParentSize(); Insert(0, bg);
        }

        private void AddGroup(LinesGroup group)
        {
            string groupName = group.title.ToLower();

            if (!groups.ContainsKey(groupName))
            {
                GroupErrorData groupErrorData = new();
                groupErrorData.Groups.Add(group);

                groups.Add(groupName, groupErrorData);
                return;
            }

            List<LinesGroup> groupsList = groups[groupName].Groups;

            groupsList.Add(group);

            Color errorColor = groups[groupName].ErrorData.Color;

            group.SetErrorStyle(errorColor);

            if (groupsList.Count == 2)
            {
                ++NameErrorsCount;

                groupsList[0].SetErrorStyle(errorColor);
            }
        }

        // === Manipulators & Menus ===
        private void AddManipulators()
        {
            // 1) Top‐level context menu entries:
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var menu = evt.menu;
                // Only show when there are nodes to group
                if (selection.Any(x => x is LineNode))
                {
                    // Group selected nodes
                    menu.AppendAction("Group Selected Nodes", action =>
                    {
                        var group = CreateGroupAt("New Group", action.eventInfo.localMousePosition);
                        foreach (var sel in selection.ToList()) // <- Copy the selection to ensure we can move over it while setting elements to the group
                            if (sel is LineNode node)
                            {
                                try
                                {
                                    node.Group = group;
                                    group.AddElement(node);
                                }
                                catch
                                {
                                }
                            }
                    }, DropdownMenuAction.Status.Normal);
                }

                // Only show when right‐clicking on empty canvas
                if (evt.target is GraphView || evt.target == contentViewContainer)
                {
                    // Create empty group
                    menu.AppendAction("Create Group Here", action =>
                    {
                        var mousePos = evt.localMousePosition;
                        CreateGroupAt("New Group", action.eventInfo.localMousePosition);
                    }, DropdownMenuAction.Status.Normal);

                    // Separator
                    menu.AppendSeparator();
                }
            }));

            // 2) Preserve your zoom/drag/select manipulators:
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            nodeCreationRequest += (NodeCreationContext ctx) =>
            {
                CreateNodeAt(ctx.screenMousePosition);
            };
        }

        private void AddMiniMap()
        {
            miniMap = new MiniMap
            {
                anchored = true // Important: makes it track the GraphView content properly
            };

            Rect defaultRect = new(0, 40, 200, 180);
            miniMap.SetPosition(defaultRect);
            miniMap.visible = false;

            Add(miniMap);

            // Add resize functionality
            AddMiniMapResizeHandle();
        }

        private void AddMiniMapResizeHandle()
        {
            var resizeHandle = new VisualElement();
            resizeHandle.style.position = Position.Absolute;
            resizeHandle.style.width = 8;
            resizeHandle.style.height = 8;
            resizeHandle.style.bottom = 0;
            resizeHandle.style.right = 0;
            resizeHandle.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            resizeHandle.style.cursor = UnityDefaultCursor.DefaultCursor(MouseCursor.ResizeUpLeft);
            resizeHandle.name = "miniMapResizeHandle";

            miniMap.Add(resizeHandle);

            bool resizing = false;
            Vector2 startMouse = default;
            Rect startRect = Rect.zero;

            resizeHandle.RegisterCallback<MouseDownEvent>(e =>
            {
                resizing = true;
                startMouse = e.mousePosition;
                startRect = miniMap.GetPosition();

                resizeHandle.CaptureMouse(); // <-- capture mouse input
                e.StopPropagation();
            });

            resizeHandle.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (!resizing || !resizeHandle.HasMouseCapture()) return;

                Vector2 delta = e.mousePosition - startMouse;
                Rect newRect = startRect;
                newRect.width = Mathf.Max(100, startRect.width + delta.x);
                newRect.height = Mathf.Max(100, startRect.height + delta.y);

                miniMap.SetPosition(newRect);
                e.StopPropagation();
            });

            resizeHandle.RegisterCallback<MouseUpEvent>(e =>
            {
                if (!resizing) return;

                resizing = false;
                if (resizeHandle.HasMouseCapture())
                    resizeHandle.ReleaseMouse(); // <-- release after resize ends

                e.StopPropagation();
            });
        }

        private void AddMiniMapStyles()
        {
            var bg = new Color32(29, 29, 30, 255);
            var border = new Color32(51, 51, 51, 255);
            miniMap.style.backgroundColor = new StyleColor(bg);
            miniMap.style.borderTopColor = new StyleColor(border);
            miniMap.style.borderRightColor = new StyleColor(border);
            miniMap.style.borderBottomColor = new StyleColor(border);
            miniMap.style.borderLeftColor = new StyleColor(border);
        }

        private void AddSearchWindow()
        {
            if (searchWindow == null)
                searchWindow = ScriptableObject.CreateInstance<DialogSearchWindow>();
            searchWindow.Initialize(this);
            //nodeCreationRequest = ctx => SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), searchWindow);
        }

        private void AddStyles()
        {
            this.AddStyleSheets("DialogGraphViewStyles", "DialogNodeStyles");
        }

        private void AddToolbar()
        {
            var toolbar = new Toolbar();
            saveButton = toolbar.CreateButton("Save", Save);
            toolbar.CreateButton("Clear", ClearGraph);
            toolbar.CreateButton("Reset", ResetView);
            miniMapButton = toolbar.CreateButton("Minimap", ToggleMiniMap);
            toolbar.AddStyleSheets("DialogToolbarStyles");
            Add(toolbar);
        }

        private LineNode CreateAndRecordNode(Vector2 screenPos, bool suppressInitialize = false)
        {
            var local = GetLocalMousePosition(screenPos, true);
            var node = CreateNode(local, suppressInitialize);
            AddElement(node);
            // Draw and track
            OnGraphChanged();
            return node;
        }

        private void RemoveGroup(LinesGroup group)
        {
            string oldGroupName = group.OldTitle.ToLower();

            Debug.Log(string.Join(',', groups.Select(x => x.Key + ":" + x.Value)));
            List<LinesGroup> groupsList = groups[oldGroupName].Groups;

            groupsList.Remove(group);

            group.ResetStyle();

            if (groupsList.Count == 1)
            {
                --NameErrorsCount;

                groupsList[0].ResetStyle();

                return;
            }

            if (groupsList.Count == 0)
            {
                groups.Remove(oldGroupName);
            }
        }

        private void ResetView()
        {
            ClearGraph();
            editorWindow.ClearSelectedDialog();
        }

        private void Save()
        {
            if (NameErrorsCount > 0 || RootNodesCount > 1 || !nodes.Any())
                return;
            editorWindow.repository.Save();
        }

        private void SetupOnElementsDeleted()
        {
            deleteSelection = (op, ask) =>
            {
                // error cleanup moved here
                var groupsToDelete = new List<LinesGroup>();
                var nodesToDelete = new List<LineNode>();
                var edgesToDelete = new List<Edge>();

                foreach (var elem in selection)
                {
                    switch (elem)
                    {
                        case LinesGroup g: groupsToDelete.Add(g); break;
                        case LineNode n: nodesToDelete.Add(n); break;
                        case Edge e: edgesToDelete.Add(e); break;
                    }
                }

                // delete groups
                foreach (var g in groupsToDelete)
                {
                    foreach (var n in g.containedElements.OfType<LineNode>().ToList())
                        g.RemoveElement(n);
                    RemoveGroup(g);
                    RemoveElement(g);
                }
                // delete edges
                DeleteElements(edgesToDelete);
                // delete nodes
                foreach (var n in nodesToDelete)
                {
                    try // I don't know why, but there's a bug with scopes and I don't know how to fix it.
                    {
                        n.Group?.RemoveElement(n);
                    }
                    catch
                    {
                    }
                    try
                    {
                        n.DisconnectAllPorts();
                        RemoveElement(n);
                    }
                    catch
                    {
                    }
                }
                OnGraphChanged();
            };
        }

        // === Context & Change Callbacks ===
        private void SetupOnGraphViewChanged()
        {
            graphViewChanged = changes =>
            {
                // Update response data on edge create/remove
                if (changes.edgesToCreate != null)
                {
                    foreach (Edge edge in changes.edgesToCreate)
                    {
                        var nextNode = (LineNode)edge.input.node;
                        var resp = (DialogResponseData)edge.output.userData;
                        resp.NodeID = nextNode.ID;
                    }
                }
                if (changes.elementsToRemove != null)
                {
                    foreach (var element in changes.elementsToRemove.OfType<Edge>())
                    {
                        var resp = (DialogResponseData)element.output.userData;
                        resp.NodeID = string.Empty;
                    }
                }
                OnGraphChanged();
                return changes;
            };
        }

        private void SetupOnGroupRenamed()
        {
            groupTitleChanged = (group, newTitle) =>
            {
                var dsGroup = (LinesGroup)group;

                // error tracking logic kept here
                if (string.IsNullOrEmpty(dsGroup.title) && !string.IsNullOrEmpty(dsGroup.OldTitle))
                    NameErrorsCount++;
                else if (!string.IsNullOrEmpty(dsGroup.title) && string.IsNullOrEmpty(dsGroup.OldTitle))
                    NameErrorsCount--;

                RemoveGroup(dsGroup);
                dsGroup.OldTitle = dsGroup.title;
                AddGroup(dsGroup);
                OnGraphChanged();
            };
        }

        private void UpdateMiniMapPosition()
        {
            if (miniMap == null || editorWindow == null) return;

            var currentRect = miniMap.GetPosition();

            // Top-left corner, but offset by sidebar width
            Rect targetRect = new(
                10, // X offset
                40, // Y offset (below toolbar)
                currentRect.width,
                currentRect.height
            );

            miniMap.SetPosition(targetRect);
        }

        private void ValidateRootNodes()
        {
            // 1) Clear previous root‐error styles
            foreach (var n in nodeErrors.Nodes)
                n.ResetStyle();

            // 2) Find all nodes with no incoming edges
            var allNodes = nodes.ToList(); // or cache your List<LineNode>
            nodeErrors.Nodes = allNodes
                .Where(n => !n.inputContainer.Children()
                     .OfType<Port>()
                     .Any(p => p.connected))
                .OfType<LineNode>()
                .ToList();

            // 3) If there’s not exactly one, mark as an error
            RootNodesCount = nodeErrors.Nodes.Count;
            if (nodeErrors.Nodes.Count > 1)
            {
                foreach (var node in nodeErrors.Nodes)
                {
                    node.SetErrorStyle(nodeErrors.ErrorData.Color);
                }
            }
        }

        public static class UnityDefaultCursor
        {
            private static PropertyInfo _defaultCursorId;

            private static PropertyInfo DefaultCursorId
            {
                get
                {
                    if (_defaultCursorId != null) return _defaultCursorId;
                    _defaultCursorId = typeof(UnityEngine.UIElements.Cursor).GetProperty("defaultCursorId", BindingFlags.NonPublic | BindingFlags.Instance);
                    return _defaultCursorId;
                }
            }

            public static UnityEngine.UIElements.Cursor DefaultCursor(MouseCursor cursorType)
            {
                var ret = (object)new UnityEngine.UIElements.Cursor();
                DefaultCursorId.SetValue(ret, (int)cursorType);
                return (UnityEngine.UIElements.Cursor)ret;
            }
        }
    }
}