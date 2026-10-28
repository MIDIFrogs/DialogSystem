namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>
    /// A parsed condition such as <c>Gold &gt; 10 &amp;&amp; !QuestCompleted || Role == "Test"</c>. An empty source is
    /// always true.
    /// </summary>
    public sealed class DialogExpression
    {
        private readonly DialogFunctions functions;

        private DialogExpression(string source, ExpressionNode root, DialogFunctions functions)
        {
            Source = source;
            Root = root;
            this.functions = functions;
        }

        public string Source { get; }

        public ExpressionNode Root { get; }

        public DialogFunctions Functions => functions;

        /// <summary>An expression that is always <paramref name="value"/> (used for broken or empty sources).</summary>
        public static DialogExpression Constant(bool value) =>
            new(value ? "true" : "false", new LiteralNode(DialogValue.FromBool(value), 0, 0, value ? "true" : "false"),
                new DialogFunctions());

        /// <summary>Returns null and sets <paramref name="error"/> when the source does not parse.</summary>
        public static DialogExpression Parse(string source, DialogFunctions functions, out ExpressionError error)
        {
            error = null;
            source ??= string.Empty;
            functions ??= new DialogFunctions();
            if (string.IsNullOrWhiteSpace(source))
            {
                return new DialogExpression(source, new LiteralNode(DialogValue.FromBool(true), 0, 0, string.Empty), functions);
            }

            try
            {
                return new DialogExpression(source, new ExpressionParser(source, functions).ParseExpression(), functions);
            }
            catch (ExpressionParseException exception)
            {
                error = new ExpressionError(exception.Position, exception.Length, exception.Message);
                return null;
            }
        }

        public bool Evaluate(IDialogContext context) => Root.Evaluate(context, functions).IsTruthy;
    }
}
