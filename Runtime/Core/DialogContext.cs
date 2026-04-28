using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MIDIFrogs.DialogSystem.Core
{
    public class DialogContext : IDialogContext
    {
        private readonly Dictionary<string, object> values = new();

        public IReadOnlyCollection<KeyValuePair<string, object>> Records => values; 

        public T GetValue<T>(string key)
        {
            return values.TryGetValue(key, out var value) ? (T)value : ThrowKeyNotFoundException<T>();
        }

        public void SetValue<T>(string key, T value) => values[key] = value;

        public bool TryGetValue<T>(string key, out T value)
        {
            if (values.TryGetValue(key, out var v) && v is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        [DoesNotReturn]
        private static T ThrowKeyNotFoundException<T>() => throw new KeyNotFoundException();
    }
}