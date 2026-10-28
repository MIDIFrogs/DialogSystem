using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core
{
    public class DialogContext : IDialogContext
    {
        private readonly Dictionary<string, object> values = new();

        public IEnumerable<KeyValuePair<string, object>> Records => values;

        public bool TryGetValue(string key, out object value) => values.TryGetValue(key, out value);

        public void SetValue(string key, object value) => values[key] = value;
    }
}
