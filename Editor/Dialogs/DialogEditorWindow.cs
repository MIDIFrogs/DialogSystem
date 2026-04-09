using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using MIDIFrogs.DialogSystem.Editor.Dialogs;
using MIDIFrogs.DialogSystem.Editor.Utilities;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Editor;

#pragma warning disable CS0618
public class DialogEditorWindow : EditorWindow
{
    internal DialogGraphRepository repository;
    internal DialogAuthorAsset selectedAuthor;
    internal DialogAsset selectedDialog;

    private Tab authorsTab;
    private Tab dialogsTab;
    private DialogGraphView dialogView;
    private VisualElement root;
    private Tabs selectedTab;
    private ListView sideView;

    private enum Tabs { Dialogs, Authors }

    internal float SidebarWidth
    {
        get
        {
            var split = rootVisualElement.Q<TwoPaneSplitView>();
            return split != null ? split.fixedPaneInitialDimension : 250;
        }
    }

    private DialogEditorSettings Settings => DialogEditorSettings.GetOrCreateSettings();

    [MenuItem("Window/Dialog Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<DialogEditorWindow>("Dialog Editor");
        window.minSize = new Vector2(800, 600);
    }

    public void ClearSelectedDialog()
    {
        if (selectedTab == Tabs.Dialogs) sideView.ClearSelection();
        else OnDialogSelected(null);
    }

    private void AddAuthorOrDialog()
    {
        switch (selectedTab)
        {
            case Tabs.Dialogs: CreateNewDialog(); break;
            case Tabs.Authors: CreateNewAuthor(); break;
        }
    }

    private Tab AddAuthorsTab()
    {
        authorsTab = new Tab("Authors");
        authorsTab.selected += ReloadAuthors;
        return authorsTab;
    }

    private Tab AddDialogsTab()
    {
        dialogsTab = new Tab("Dialogs");
        dialogsTab.selected += ReloadDialogs;
        return dialogsTab;
    }

    private void AddStyles()
    {
        rootVisualElement.AddStyleSheets("DialogVariables");
    }

    private void BindAuthorCard(VisualElement authorPanel, DialogAuthorAsset author)
    {
        authorPanel.Clear();
        authorPanel.style.flexDirection = FlexDirection.Row;
        authorPanel.style.alignItems = Align.Center;

        var avatarPreview = new Image { sprite = author.Avatar, style = { width = 24, height = 24 } };
        authorPanel.Add(avatarPreview);

        var avatarField = new ObjectField { objectType = typeof(Sprite), value = author.Avatar, style = { width = 20 } };
        avatarField.RegisterValueChangedCallback(e => { author.Avatar = avatarPreview.sprite = (Sprite)e.newValue; EditorUtility.SetDirty(author); });
        authorPanel.Add(avatarField);

        var nameCard = new RenamableCard(author.DisplayName) { TextColor = author.NameColor };
        nameCard.OnRenamed += newText => 
        {
            var oldPath = AssetDatabase.GetAssetPath(author);
            AssetDatabase.RenameAsset(oldPath, newText);
            author.DisplayName = newText;
            EditorUtility.SetDirty(author);
        };
        nameCard.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete author", action =>
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(author));
                ReloadAuthors(authorsTab);
            });
        }));
        nameCard.style.alignSelf = Align.Center;
        authorPanel.Add(nameCard);
    }

    private void BindDialogCard(VisualElement dialogPanel, DialogAsset dialog)
    {
        dialogPanel.Clear();
        dialogPanel.style.flexDirection = FlexDirection.Row;
        dialogPanel.style.alignItems = Align.Center;

        var nameCard = new RenamableCard(dialog.name);
        nameCard.OnRenamed += newText =>
        {
            var oldPath = AssetDatabase.GetAssetPath(dialog);
            var cachePath = DialogGraphRepository.GetCachePath(dialog);
            AssetDatabase.RenameAsset(oldPath, newText);
            if (AssetDatabase.AssetPathExists(cachePath))
                AssetDatabase.RenameAsset(cachePath, newText);
        };
        nameCard.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete dialog", action =>
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(dialog));
                var cachePath = DialogGraphRepository.GetCachePath(dialog);
                AssetDatabase.DeleteAsset(cachePath);
                ReloadDialogs(dialogsTab);
            });
        }));
        nameCard.style.alignSelf = Align.Stretch;
        dialogPanel.Add(nameCard);
    }

    private VisualElement CreateGraph()
    {
        dialogView = new DialogGraphView(this);
        dialogView.SetEnabled(false);
        return dialogView;
    }

    private static void EnsureFolderPathExists(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var parts = path.Split('/');
        string accum = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = parts[i];
            if (string.IsNullOrEmpty(next)) continue;
            var candidate = $"{accum}/{next}";
            if (!AssetDatabase.IsValidFolder(candidate))
                AssetDatabase.CreateFolder(accum, next);
            accum = candidate;
        }
    }

    private void CreateNewAuthor()
    {
        var settings = Settings;
        EnsureFolderPathExists(settings.AuthorsPath);
        var newAuthor = CreateInstance<DialogAuthorAsset>();
        newAuthor.DisplayName = "<New Author>";
        newAuthor.NameColor = Color.white;
        newAuthor.Id = Guid.NewGuid().ToString();
        AssetDatabase.CreateAsset(newAuthor, $"{settings.AuthorsPath}/NewAuthor.asset");
        AssetDatabase.SaveAssets();
        ReloadAuthors(authorsTab);
    }

    private void CreateNewDialog()
    {
        var settings = Settings;
        EnsureFolderPathExists(settings.DialogsPath);
        var newDialog = CreateInstance<DialogAsset>();
        AssetDatabase.CreateAsset(newDialog, $"{settings.DialogsPath}/NewDialog.asset");
        AssetDatabase.SaveAssets();
        ReloadDialogs(dialogsTab);
    }

    private VisualElement CreateTabs()
    {
        var tabs = new TabView();
        tabs.Add(AddDialogsTab());
        tabs.Add(AddAuthorsTab());

        var header = tabs.Q("unity-tab-view__header-container");
        var addButton = new Button(AddAuthorOrDialog) { text = "+", style = { width = 20, height = 20, marginLeft = Length.Auto() } };
        header.Add(addButton);

        return tabs;
    }

    private void CreateUI()
    {
        root = rootVisualElement;
        var splitView = new TwoPaneSplitView
        {
            orientation = TwoPaneSplitViewOrientation.Horizontal,
            fixedPaneInitialDimension = 250,
        };
        splitView.Add(CreateTabs());
        splitView.Add(CreateGraph());
        root.Add(splitView);
        AddStyles();

        ReloadAuthors(authorsTab);
        ReloadDialogs(dialogsTab);
    }

    private void FocusInExplorer(UnityEngine.Object obj)
    {
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = obj;
    }

    private void OnAuthorSelected(DialogAuthorAsset author)
    {
        selectedAuthor = author;
        FocusInExplorer(author);
    }

    private void OnDialogSelected(DialogAsset dialog)
    {
        selectedDialog = dialog;
        if (dialog == null)
        {
            dialogView.ClearGraph();
            dialogView.SetEnabled(false);
            return;
        }

        repository = new DialogGraphRepository(dialogView, dialog);
        repository.Load();
        FocusInExplorer(dialog);
        dialogView.SetEnabled(true);
    }

    private void OnEnable()
    {
        rootVisualElement.Clear();
        CreateUI();
    }

    private void OnDisable()
    {
        if (dialogView != null)
        {
            dialogView.ClearGraph();
            dialogView.RemoveFromHierarchy();
            dialogView = null;
        }
        repository = null;

        rootVisualElement.Clear();
    }

    private void ReloadAuthors(Tab tab)
    {
        selectedTab = Tabs.Authors;
        tab.Clear();
        var settings = Settings;
        var authors = ScanFor<DialogAuthorAsset>(settings.AuthorsPath, x => x.DisplayName);
        var list = authors.ToList();
        sideView = new ListView(list, makeItem: () => new VisualElement(), bindItem: (x, i) => BindAuthorCard(x, list[i]));
        sideView.selectionChanged += items => OnAuthorSelected((DialogAuthorAsset)items.FirstOrDefault());
        tab.Add(sideView);
    }

    private void ReloadDialogs(Tab tab)
    {
        selectedTab = Tabs.Dialogs;
        tab.Clear();
        var settings = Settings;
        var dialogs = ScanFor<DialogAsset>(settings.DialogsPath);
        var list = dialogs.ToList();
        sideView = new ListView(list, makeItem: () => new VisualElement(), bindItem: (x, i) => BindDialogCard(x, list[i]));
        sideView.selectionChanged += items => OnDialogSelected((DialogAsset)items.FirstOrDefault());
        tab.Add(sideView);
    }

    private List<T> ScanFor<T>(string path, Func<T, string> compareKeyExtractor = null) where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T)}", new[] { path })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .OrderBy(x => compareKeyExtractor?.Invoke(x) ?? x.name)
            .ToList();
    }

    private static class ContextMenuBinder
    {
        private static readonly Dictionary<VisualElement, IManipulator> bindings = new();
        public static void Bind(VisualElement element, IManipulator binding)
        {
            bindings[element] = binding;
            element.AddManipulator(binding);
        }
        public static void Clear(VisualElement element)
        {
            if (bindings.Remove(element, out var binding))
                element.RemoveManipulator(binding);
        }
    }
}