namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    public sealed class OrNode : ExpressionNode
    {
        public OrNode(ExpressionNode left, ExpressionNode right, int position, int length, string source)
            : base(position, length, source)
        {
            Left = left;
            Right = right;
        }

        public ExpressionNode Left { get; }

        public ExpressionNode Right { get; }

        public override DialogValue Evaluate(IDialogContext context, DialogFunctions functions) =>
            DialogValue.FromBool(Left.Evaluate(context, functions).IsTruthy || Right.Evaluate(context, functions).IsTruthy);
    }
}
