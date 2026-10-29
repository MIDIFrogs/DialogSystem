using MIDIFrogs.DialogSystem.Core.Expressions;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>An authoring problem. Warnings decorate the graph and never block saving.</summary>
    public sealed class DialogWarning
    {
        public DialogWarning(string lineId, string answerId, string field, string message, ExpressionError error = null)
        {
            LineId = lineId;
            AnswerId = answerId;
            Field = field;
            Message = message;
            Error = error;
        }

        /// <summary>The line, or null for the dialog itself (Start, groups).</summary>
        public string LineId { get; }

        public string AnswerId { get; }

        /// <summary>The asset field name, e.g. <c>EnabledIf</c> or <c>NextId</c>.</summary>
        public string Field { get; }

        public string Message { get; }

        /// <summary>The parse error with its position, for expression fields.</summary>
        public ExpressionError Error { get; }

        public override string ToString() => Message;
    }
}
