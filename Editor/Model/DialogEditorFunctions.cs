using MIDIFrogs.DialogSystem.Core.Expressions;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>
    /// Functions the dialog editor knows when it checks expressions. Register the game's functions here from an
    /// <c>[InitializeOnLoad]</c> editor class (the same calls you make on <c>DialogService.Functions</c>); unknown
    /// functions are reported as errors.
    /// </summary>
    public static class DialogEditorFunctions
    {
        public static DialogFunctions Functions { get; } = new();
    }
}
