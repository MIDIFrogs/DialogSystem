namespace MIDIFrogs.DialogSystem.Editor.Window
{
    internal enum ChangeKind
    {
        /// <summary>Text typed into a field: one Undo step per typing pause.</summary>
        Typing,

        /// <summary>A value changed: views refresh in place.</summary>
        Value,

        /// <summary>Lines, answers, links or groups changed: the graph is rebuilt from the asset.</summary>
        Structure,
    }
}
