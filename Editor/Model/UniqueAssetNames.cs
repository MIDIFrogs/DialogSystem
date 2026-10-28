using UnityEditor;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>Paths for new assets that never overwrite an existing one ("NewDialog 1.asset").</summary>
    public static class UniqueAssetNames
    {
        public static string NewAssetPath(string folder, string baseName) =>
            AssetDatabase.GenerateUniqueAssetPath($"{folder.TrimEnd('/')}/{baseName}.asset");
    }
}
