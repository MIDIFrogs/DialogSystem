namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    public sealed class LiteralNode : ExpressionNode
    {
        public LiteralNode(DialogValue value, int position, int length, string source) : base(position, length, source)
        {
            Value = value;
        }

        public DialogValue Value { get; }

        public override DialogValue Evaluate(IDialogContext context, DialogFunctions functions) => Value;
    }
}
