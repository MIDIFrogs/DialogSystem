using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core
{
    /// <summary>A visible answer of the current line, whether it can be picked, and what it still needs.</summary>
    public sealed class ChoiceState
    {
        public ChoiceState(DialogChoice choice, bool enabled, IReadOnlyList<string> hint)
        {
            Choice = choice;
            Enabled = enabled;
            Hint = hint;
        }

        public DialogChoice Choice { get; }

        public bool Enabled { get; }

        public IReadOnlyList<string> Hint { get; }
    }
}
