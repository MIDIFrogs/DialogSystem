namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    public sealed class VariableNode : ExpressionNode
    {
        public VariableNode(string name, int position, int length, string source) : base(position, length, source)
        {
            Name = name;
        }

        public string Name { get; }

        public override DialogValue Evaluate(IDialogContext context, DialogFunctions functions) =>
            context != null && context.TryGetValue(Name, out object value) ? DialogValue.From(value) : DialogValue.Unset;
    }
}
