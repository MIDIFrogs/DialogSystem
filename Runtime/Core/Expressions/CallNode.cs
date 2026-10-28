using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    public sealed class CallNode : ExpressionNode
    {
        public CallNode(DialogFunction function, IReadOnlyList<ExpressionNode> arguments, int position, int length, string source)
            : base(position, length, source)
        {
            Function = function;
            Arguments = arguments;
        }

        public DialogFunction Function { get; }

        public IReadOnlyList<ExpressionNode> Arguments { get; }

        public override DialogValue Evaluate(IDialogContext context, DialogFunctions functions)
        {
            var values = new DialogValue[Arguments.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = Arguments[i].Evaluate(context, functions);
            }
            return Function.Body(values);
        }
    }
}
