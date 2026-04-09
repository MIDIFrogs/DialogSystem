using UnityEditor;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Editor
{
    // Editor-only settings for dialog editor paths
    public class DialogEditorSettings : ScriptableObject
    {
        [Header("Default asset paths")]
        public string DialogsPath = "Assets/Dialogs";
        public string TriggersPath = "Assets/Dialogs/Triggers";
        public string AuthorsPath = "Assets/Dialogs/Authors";

        [Header("Cache settings")]
        public string CachePath = "Assets/Editor/Dialogs/DialogsCache";

        private const string DefaultSettingsFolder = "Assets/Editor/Dialogs";
        private const string DefaultSettingsAssetPath = DefaultSettingsFolder + "/DialogEditorSettings.asset";

        public static DialogEditorSettings GetOrCreateSettings()
        {
            // Try to find existing settings by type
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(DialogEditorSettings));
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var found = AssetDatabase.LoadAssetAtPath<DialogEditorSettings>(path);
                if (found != null) return found;
            }

            // Ensure default folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");
            if (!AssetDatabase.IsValidFolder(DefaultSettingsFolder))
                AssetDatabase.CreateFolder("Assets/Editor", "Dialogs");

            // Create default settings asset
            var settings = ScriptableObject.CreateInstance<DialogEditorSettings>();
            AssetDatabase.CreateAsset(settings, DefaultSettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return settings;
        }

        [MenuItem("Window/Dialog Editor/Settings")]
        public static void OpenSettingsAsset()
        {
            var settings = GetOrCreateSettings();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}