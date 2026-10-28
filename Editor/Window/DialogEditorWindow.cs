using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Editor.Model;
using MIDIFrogs.DialogSystem.Editor.Settings;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Window
{
    /// <summary>
    /// The dialog editor: dialog list and author palette on the left, the graph in the centre, the properties of
    /// the selected line on the right. Every change is undoable and saved about half a second after the last one.
    /// </summary>
    public sealed class DialogEditorWindow : EditorWindow
    {
        private const double AutosaveDelay = 0.5;

        [SerializeField] private string dialogGuid;
        [SerializeField] private string authorGuid;
        [SerializeField] private bool showMiniMap = true;
        [SerializeField] private bool showProperties = true;

        private readonly List<DialogAuthorAsset> authors = new();
        private DialogEditorSettings settings;
        private DialogAsset dialog;
        private DialogAuthorAsset selectedAuthor;
        private Debouncer debouncer;
        private int typingGroup = -1;
        private string typingKey;
        private bool reloadScheduled;
        private string selectedLineId;
        private string pendingSelection;
        private IReadOnlyList<DialogWarning> warnings = Array.Empty<DialogWarning>();

        private DialogGraphView graph;
        private PropertiesPanel panel;
        private SidebarList dialogList;
        private SidebarList authorList;
        private TwoPaneSplitView centreSplit;
        private Label status;
        private ToolbarButton sinkButton;

        internal DialogAsset Dialog => dialog;

        internal DialogAuthorAsset SelectedAuthor => selectedAuthor;

        internal IReadOnlyList<DialogAuthorAsset> Authors => authors;

        internal DialogFunctions Functions => DialogEditorFunctions.Functions;

        internal DialogGraphView Graph => graph;

        internal bool HasPendingSave => debouncer != null && debouncer.Pending;

        [MenuItem("Window/Dialog Editor/Editor")]
        public static void Open() => GetWindow<DialogEditorWindow>("Dialog Editor");

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is not DialogAsset asset)
            {
                return false;
            }

            DialogEditorWindow window = GetWindow<DialogEditorWindow>("Dialog Editor");
            window.OpenDialog(asset);
            return true;
        }

        internal static string AuthorName(DialogAuthorAsset author)
        {
            if (author == null)
            {
                return "Без автора";
            }
            return string.IsNullOrEmpty(author.DisplayName) ? author.name : author.DisplayName;
        }

        /// <summary>Applies an edit to the dialog with Undo. Typing into one field merges into one Undo step per pause.</summary>
        internal void Change(string action, Action<DialogAsset> apply, ChangeKind kind, string fieldKey = null)
        {
            if (dialog == null)
            {
                return;
            }

            DialogGraphEditing.Record(dialog, action);
            apply(dialog);
            if (kind == ChangeKind.Typing && typingGroup >= 0 && fieldKey == typingKey)
            {
                Undo.CollapseUndoOperations(typingGroup);
            }
            else if (kind == ChangeKind.Typing)
            {
                typingGroup = Undo.GetCurrentGroup();
                typingKey = fieldKey;
            }
            else
            {
                EndTyping();
            }
            AfterChange(kind);
        }

        /// <summary>Call after editing the dialog (with Undo recorded) to refresh the views and schedule the save.</summary>
        internal void AfterChange(ChangeKind kind)
        {
            if (dialog == null)
            {
                return;
            }

            EditorUtility.SetDirty(dialog);
            debouncer.Poke();
            SetSaved(false);
            if (kind == ChangeKind.Structure)
            {
                EndTyping();
                ScheduleReload();
                return;
            }

            graph.RefreshNodes(dialog);
            panel.Refresh();
            Validate();
        }

        internal void AddAnswer(string lineId)
        {
            if (dialog == null)
            {
                return;
            }

            DialogAsset.Answer answer = DialogGraphEditing.AddAnswer(dialog, lineId);
            if (answer != null)
            {
                panel.OpenAnswer(answer.Id);
                SelectLineAfterReload(lineId);
                AfterChange(ChangeKind.Structure);
            }
        }

        internal void RemoveAnswer(string lineId, string answerId)
        {
            if (dialog == null)
            {
                return;
            }

            DialogGraphEditing.RemoveAnswer(dialog, lineId, answerId);
            SelectLineAfterReload(lineId);
            AfterChange(ChangeKind.Structure);
        }

        internal void SelectLineAfterReload(string lineId) => pendingSelection = lineId;

        internal void SelectLine(string lineId) => graph.SelectLine(lineId);

        internal void OnLineSelected(string lineId)
        {
            if (lineId == selectedLineId && panel.childCount > 0)
            {
                return;
            }

            selectedLineId = lineId;
            EndTyping();
            panel.Show(lineId);
            panel.ShowWarnings(warnings);
        }

        private void OnEnable()
        {
            debouncer = new Debouncer(AutosaveDelay, () => EditorApplication.timeSinceStartup);
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += Tick;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            SaveNow();
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= Tick;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        public void CreateGUI()
        {
            settings = DialogEditorSettings.GetOrCreateSettings();
            VisualElement root = rootVisualElement;
            var styles = Resources.Load<StyleSheet>("DialogEditor");
            if (styles != null)
            {
                root.styleSheets.Add(styles);
            }
            root.AddToClassList("dialog-editor");

            root.Add(BuildToolbar());

            dialogList = new SidebarList("Диалоги", CreateDialog);
            authorList = new SidebarList("Авторы", CreateAuthor);
            var sidebar = new TwoPaneSplitView(0, 220, TwoPaneSplitViewOrientation.Vertical);
            sidebar.AddToClassList("dialog-side");
            sidebar.Add(dialogList);
            sidebar.Add(authorList);

            graph = new DialogGraphView(this);
            graph.SetMiniMapVisible(showMiniMap);
            panel = new PropertiesPanel(this);
            centreSplit = new TwoPaneSplitView(1, 300, TwoPaneSplitViewOrientation.Horizontal);
            centreSplit.Add(graph);
            centreSplit.Add(panel);

            var main = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            main.style.flexGrow = 1;
            main.Add(sidebar);
            main.Add(centreSplit);
            root.Add(main);

            if (!showProperties)
            {
                root.schedule.Execute(() => centreSplit.CollapseChild(1));
            }

            RefreshLists();
            string path = AssetDatabase.GUIDToAssetPath(dialogGuid ?? string.Empty);
            OpenDialog(string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<DialogAsset>(path));
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();
            var miniMap = new ToolbarToggle { text = "Мини-карта", value = showMiniMap };
            miniMap.RegisterValueChangedCallback(evt =>
            {
                showMiniMap = evt.newValue;
                graph.SetMiniMapVisible(showMiniMap);
            });
            toolbar.Add(miniMap);
            toolbar.Add(new ToolbarButton(() => graph.FrameAll()) { text = "Показать всё" });
            var properties = new ToolbarToggle { text = "Свойства", value = showProperties };
            properties.RegisterValueChangedCallback(evt =>
            {
                showProperties = evt.newValue;
                if (showProperties)
                {
                    centreSplit.UnCollapse();
                }
                else
                {
                    centreSplit.CollapseChild(1);
                }
            });
            toolbar.Add(properties);
            toolbar.Add(new ToolbarSpacer { flex = true });
            sinkButton = new ToolbarButton(ExportStrings)
            {
                text = "Обновить таблицу строк",
                tooltip = "Записать тексты всех диалогов и имена авторов в таблицу строк из настроек",
            };
            toolbar.Add(sinkButton);
            status = new Label();
            status.AddToClassList("dialog-status");
            toolbar.Add(status);
            SetSaved(true);
            return toolbar;
        }

        private void Tick()
        {
            if (debouncer != null && debouncer.Due())
            {
                SaveNow();
            }

            if (sinkButton != null)
            {
                bool hasSink = settings != null && settings.Sink != null;
                sinkButton.style.display = hasSink ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void SaveNow()
        {
            debouncer?.Cancel();
            EndTyping();
            if (dialog != null && EditorUtility.IsDirty(dialog))
            {
                AssetDatabase.SaveAssetIfDirty(dialog);
            }
            SetSaved(true);
        }

        private void EndTyping()
        {
            typingGroup = -1;
            typingKey = null;
        }

        private void SetSaved(bool saved)
        {
            if (status == null)
            {
                return;
            }

            status.text = saved ? "● Сохранено" : "● Изменено";
            status.EnableInClassList("dialog-status--dirty", !saved);
        }

        private void OnUndoRedo()
        {
            if (dialog == null || graph == null)
            {
                return;
            }

            EndTyping();
            EditorUtility.SetDirty(dialog);
            debouncer.Poke();
            SetSaved(false);
            ScheduleReload();
        }

        private void OnProjectChanged()
        {
            if (graph == null)
            {
                return;
            }

            RefreshLists();
            if (dialog == null && !string.IsNullOrEmpty(dialogGuid))
            {
                OpenDialog(null);
            }
        }

        private void ScheduleReload()
        {
            if (reloadScheduled)
            {
                return;
            }

            reloadScheduled = true;
            rootVisualElement.schedule.Execute(() =>
            {
                reloadScheduled = false;
                Reload();
            });
        }

        private void Reload()
        {
            graph.Load(dialog);
            if (pendingSelection != null)
            {
                selectedLineId = pendingSelection;
                pendingSelection = null;
                graph.SelectLine(selectedLineId);
            }

            if (dialog == null || dialog.FindLine(selectedLineId) == null)
            {
                selectedLineId = null;
            }
            panel.Show(selectedLineId);
            Validate();
        }

        private void Validate()
        {
            warnings = dialog != null ? DialogValidator.Validate(dialog, Functions) : Array.Empty<DialogWarning>();
            graph.ShowWarnings(warnings);
            panel.ShowWarnings(warnings);
        }

        internal void OpenDialog(DialogAsset asset)
        {
            SaveNow();
            dialog = asset;
            dialogGuid = asset != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)) : null;
            selectedLineId = null;
            pendingSelection = null;
            if (graph == null)
            {
                return;
            }

            RefreshLists();
            Reload();
            if (dialog != null)
            {
                graph.schedule.Execute(() => graph.FrameAll()).ExecuteLater(50);
            }
        }

        private void RefreshLists()
        {
            dialogList.ClearItems();
            foreach (DialogAsset asset in FindAll<DialogAsset>())
            {
                DialogAsset item = asset;
                dialogList.AddItem(asset.name, null, asset == dialog, () => OpenDialog(item), () => Ping(item));
            }

            authors.Clear();
            authors.AddRange(FindAll<DialogAuthorAsset>());
            string authorPath = AssetDatabase.GUIDToAssetPath(authorGuid ?? string.Empty);
            selectedAuthor = string.IsNullOrEmpty(authorPath) ? null : AssetDatabase.LoadAssetAtPath<DialogAuthorAsset>(authorPath);

            authorList.ClearItems();
            foreach (DialogAuthorAsset author in authors)
            {
                DialogAuthorAsset item = author;
                authorList.AddItem(AuthorName(author), author.NameColor, author == selectedAuthor, () => SelectAuthor(item), () => Ping(item));
            }
            VisualElement none = authorList.AddItem("Без автора", new Color(0.3f, 0.3f, 0.3f), selectedAuthor == null, () => SelectAuthor(null), null);
            none.AddToClassList("dialog-side__item--none");
        }

        private void SelectAuthor(DialogAuthorAsset author)
        {
            authorGuid = author != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(author)) : null;
            RefreshLists();
        }

        private void CreateDialog()
        {
            EnsureFolder(settings.DialogsPath);
            string path = UniqueAssetNames.NewAssetPath(settings.DialogsPath, "NewDialog");
            var asset = CreateInstance<DialogAsset>();
            asset.Id = Path.GetFileNameWithoutExtension(path);
            asset.StartPosition = new Vector2(40, 200);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            OpenDialog(asset);
        }

        private void CreateAuthor()
        {
            EnsureFolder(settings.AuthorsPath);
            string path = UniqueAssetNames.NewAssetPath(settings.AuthorsPath, "NewAuthor");
            var author = CreateInstance<DialogAuthorAsset>();
            author.Id = Path.GetFileNameWithoutExtension(path);
            author.DisplayName = author.Id;
            if (settings.DefaultTypingSound != null)
            {
                author.TypingSound = (IDialogSound)JsonUtility.FromJson(JsonUtility.ToJson(settings.DefaultTypingSound),
                    settings.DefaultTypingSound.GetType());
            }
            AssetDatabase.CreateAsset(author, path);
            AssetDatabase.SaveAssetIfDirty(author);
            SelectAuthor(author);
            Ping(author);
        }

        private void ExportStrings()
        {
            IDialogStringSink sink = settings.Sink;
            if (sink == null)
            {
                return;
            }

            SaveNow();
            var strings = new List<DialogString>();
            var keys = new HashSet<string>();
            List<DialogAsset> dialogs = FindAll<DialogAsset>();
            IEnumerable<DialogAuthorAsset> authorsOnce = authors;
            foreach (DialogAsset asset in dialogs.DefaultIfEmpty(null))
            {
                foreach (DialogString item in DialogStringExport.Collect(asset, authorsOnce))
                {
                    if (keys.Add(item.Key))
                    {
                        strings.Add(item);
                    }
                }
                authorsOnce = null;
            }

            sink.Write(strings, settings.SourceLanguage);
            ShowNotification(new GUIContent($"Строк записано: {strings.Count}"));
        }

        private static void Ping(UnityEngine.Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static List<T> FindAll<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToList();

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.TrimEnd('/').Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
