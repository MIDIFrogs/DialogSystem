using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core
{
    /// <summary>Variables of one played dialog: bool, number (double) or string values by name.</summary>
    public interface IDialogContext
    {
        IEnumerable<KeyValuePair<string, object>> Records { get; }

        bool TryGetValue(string key, out object value);

        void SetValue(string key, object value);
    }
}
