namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    public sealed class NotNode : ExpressionNode
    {
        public NotNode(ExpressionNode operand, int position, int length, string source) : base(position, length, source)
        {
            Operand = operand;
        }

        public ExpressionNode Operand { get; }

        public override DialogValue Evaluate(IDialogContext context, DialogFunctions functions) =>
            DialogValue.FromBool(!Operand.Evaluate(context, functions).IsTruthy);
    }
}
