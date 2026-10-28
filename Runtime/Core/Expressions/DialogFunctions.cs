using System;
using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>Functions the game registers for expressions and action scripts.</summary>
    public sealed class DialogFunctions
    {
        private readonly Dictionary<string, DialogFunction> functions = new(StringComparer.Ordinal);

        public IEnumerable<DialogFunction> All => functions.Values;

        public void Register(string name, int argCount, Func<DialogValue[], DialogValue> body, string hint = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A function needs a name.", nameof(name));
            }

            functions[name] = new DialogFunction(name, argCount, body ?? throw new ArgumentNullException(nameof(body)), hint);
        }

        public bool TryGet(string name, out DialogFunction function) => functions.TryGetValue(name, out function);
    }
}
