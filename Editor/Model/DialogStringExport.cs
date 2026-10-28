using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Editor.Settings;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Integration;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>Collects every player-facing string of a dialog with its localization key.</summary>
    public static class DialogStringExport
    {
        public const string ContinueText = "Продолжить…";
        public const string RequiredText = "Требуется:";

        public static IReadOnlyList<DialogString> Collect(DialogAsset dialog, IEnumerable<DialogAuthorAsset> authors)
        {
            var strings = new List<DialogString>();
            var keys = new HashSet<string>();

            void Add(string key, string text)
            {
                if (!string.IsNullOrWhiteSpace(text) && keys.Add(key))
                {
                    strings.Add(new DialogString(key, text));
                }
            }

            Add(DialogKeys.Continue, ContinueText);
            Add(DialogKeys.Required, RequiredText);

            if (dialog != null)
            {
                foreach (DialogAsset.Line line in dialog.Lines)
                {
                    Add(DialogKeys.Line(dialog.Id, line.Id), line.Text);
                    foreach (DialogAsset.Answer answer in line.Answers)
                    {
                        Add(DialogKeys.Answer(dialog.Id, line.Id, answer.Id), answer.Text);
                        Add(DialogKeys.Hint(dialog.Id, line.Id, answer.Id), answer.Hint);
                    }
                }
            }

            if (authors != null)
            {
                foreach (DialogAuthorAsset author in authors)
                {
                    if (author != null && !string.IsNullOrEmpty(author.Id))
                    {
                        Add(DialogKeys.Author(author.Id), author.DisplayName);
                    }
                }
            }

            return strings;
        }
    }
}
