namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>A node of a parsed expression, with the source range it came from.</summary>
    public abstract class ExpressionNode
    {
        protected ExpressionNode(int position, int length, string source)
        {
            Position = position;
            Length = length;
            Source = source;
        }

        public int Position { get; }

        public int Length { get; }

        /// <summary>The source text of this node, e.g. "Gold >= 10".</summary>
        public string Source { get; }

        public abstract DialogValue Evaluate(IDialogContext context, DialogFunctions functions);
    }
}
