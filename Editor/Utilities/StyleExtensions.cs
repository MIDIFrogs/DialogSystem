using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MIDIFrogs.DialogSystem.Editor.Utilities
{
    public static class StyleExtensions
    {
        public static VisualElement AddClasses(this VisualElement element, params string[] classNames)
        {
            foreach (string className in classNames)
            {
                element.AddToClassList(className);
            }

            return element;
        }

        public static VisualElement AddStyleSheets(this VisualElement element, params string[] styleSheetNames)
        {
            foreach (string styleSheetName in styleSheetNames)
            {
                var guids = AssetDatabase.FindAssets($"{styleSheetName} t:StyleSheet");
                if (guids.Length == 0)
                {
                    Debug.LogWarning($"StyleSheet '{styleSheetName}' не найден в проекте.");
                    continue;
                }

                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null)
                    element.styleSheets.Add(sheet);
            }

            return element;
        }
    }
}