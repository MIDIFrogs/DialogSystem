using System;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>A game function expressions can call, e.g. HasItem("Sword").</summary>
    public sealed class DialogFunction
    {
        public DialogFunction(string name, int argCount, Func<DialogValue[], DialogValue> body, string hint)
        {
            Name = name;
            ArgCount = argCount;
            Body = body;
            Hint = hint;
        }

        public string Name { get; }

        public int ArgCount { get; }

        public Func<DialogValue[], DialogValue> Body { get; }

        /// <summary>Shown as the "required" hint when a call to this function fails; null uses its source text.</summary>
        public string Hint { get; }
    }
}
