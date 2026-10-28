using MIDIFrogs.DialogSystem.Core.Expressions;

namespace MIDIFrogs.DialogSystem.Core
{
    /// <summary>An answer: hidden while <see cref="VisibleIf"/> is false, locked while <see cref="EnabledIf"/> is false.</summary>
    public class DialogChoice
    {
        public string Id;
        public string Text;
        public string TextKey;
        public DialogExpression VisibleIf = DialogExpression.Constant(true);
        public DialogExpression EnabledIf = DialogExpression.Constant(true);

        /// <summary>Authored "required" text; replaces the generated hint when set.</summary>
        public string Hint;
        public string HintKey;
        public DialogScript Actions = DialogScript.Empty;
        public DialogNode Target;
    }
}
