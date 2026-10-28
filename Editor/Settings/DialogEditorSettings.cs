using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Settings
{
    /// <summary>Project-wide dialog editor settings, stored at <c>Assets/Editor/Dialogs/DialogEditorSettings.asset</c>.</summary>
    public class DialogEditorSettings : ScriptableObject
    {
        private const string DefaultSettingsFolder = "Assets/Editor/Dialogs";
        private const string DefaultSettingsAssetPath = DefaultSettingsFolder + "/DialogEditorSettings.asset";

        [Header("Folders")]
        [SerializeField] private string dialogsPath = "Assets/Dialogs";
        [SerializeField] private string authorsPath = "Assets/Dialogs/Authors";

        [Header("Text")]
        [Tooltip("Language the dialogs are written in. Exported strings are the source of this language.")]
        [SerializeField] private string sourceLanguage = "ru";
        [Tooltip("A ScriptableObject implementing IDialogStringSink that receives exported strings (e.g. a string table).")]
        [SerializeField] private ScriptableObject stringSink;

        [Header("Sound")]
        [Tooltip("Typing sound for authors without their own.")]
        [SerializeReference] private IDialogSound defaultTypingSound;

        public string DialogsPath => dialogsPath;

        public string AuthorsPath => authorsPath;

        public string SourceLanguage => sourceLanguage;

        public ScriptableObject StringSink
        {
            get => stringSink;
            set => stringSink = value;
        }

        /// <summary>The sink, or null when none is set or the object does not implement <see cref="IDialogStringSink"/>.</summary>
        public IDialogStringSink Sink => stringSink as IDialogStringSink;

        public IDialogSound DefaultTypingSound => defaultTypingSound;

        public static DialogEditorSettings GetOrCreateSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(DialogEditorSettings));
            if (guids.Length > 0)
            {
                var found = AssetDatabase.LoadAssetAtPath<DialogEditorSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (found != null)
                {
                    return found;
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }
            if (!AssetDatabase.IsValidFolder(DefaultSettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Editor", "Dialogs");
            }

            var settings = CreateInstance<DialogEditorSettings>();
            AssetDatabase.CreateAsset(settings, DefaultSettingsAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        [MenuItem("Window/Dialog Editor/Settings")]
        public static void OpenSettingsAsset()
        {
            DialogEditorSettings settings = GetOrCreateSettings();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        private void OnValidate()
        {
            if (stringSink != null && stringSink is not IDialogStringSink)
            {
                Debug.LogWarning($"{stringSink.name} does not implement {nameof(IDialogStringSink)}; dialog strings will not be exported.", this);
            }
        }
    }
}
