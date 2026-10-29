namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>A parenthesised expression: evaluates its inner node, keeps the parentheses in its source.</summary>
    public sealed class GroupNode : ExpressionNode
    {
        public GroupNode(ExpressionNode inner, int position, int length, string source) : base(position, length, source)
        {
            Inner = inner;
        }

        public ExpressionNode Inner { get; }

        public override DialogValue Evaluate(IDialogContext context, DialogFunctions functions) =>
            Inner.Evaluate(context, functions);
    }
}
