using MIDIFrogs.DialogSystem.Core.Expressions;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>Counts for the 👁/🔒/⚙ badges on answers.</summary>
    public static class ExpressionBadges
    {
        /// <summary>Top-level <c>&amp;&amp;</c> clauses of a condition; 0 when empty, -1 when it does not parse.</summary>
        public static int Clauses(string source, DialogFunctions functions)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return 0;
            }

            DialogExpression expression = DialogExpression.Parse(source, functions, out _);
            return expression == null ? -1 : Count(expression.Root);
        }

        /// <summary>Statements of an actions script; 0 when empty, -1 when it does not parse.</summary>
        public static int Actions(string source, DialogFunctions functions)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return 0;
            }

            DialogScript script = DialogScript.Parse(source, functions, out _);
            return script == null ? -1 : script.Count;
        }

        private static int Count(ExpressionNode node) =>
            node is AndNode and ? Count(and.Left) + Count(and.Right) : 1;
    }
}
