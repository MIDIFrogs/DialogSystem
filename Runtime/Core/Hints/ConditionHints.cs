using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Core.Expressions;

namespace MIDIFrogs.DialogSystem.Core.Hints
{
    /// <summary>
    /// Describes what a failed enabled-if condition still needs: one line per failed top-level <c>&amp;&amp;</c>
    /// clause. Comparisons read "&lt;name&gt; &lt;op&gt; &lt;value&gt;" (names through the <c>dialog.var.&lt;name&gt;</c>
    /// key), functions use their registered hint, <c>||</c> reads "одно из: a / b", anything else its source.
    /// </summary>
    public static class ConditionHints
    {
        public const string VariableKeyPrefix = "dialog.var.";

        public static IReadOnlyList<string> Describe(DialogExpression enabledIf, IDialogContext context,
            DialogFunctions functions, IDialogTextResolver text)
        {
            var lines = new List<string>();
            if (enabledIf == null || enabledIf.Evaluate(context))
            {
                return lines;
            }

            text ??= SourceTextResolver.Instance;
            var clauses = new List<ExpressionNode>();
            CollectAnd(enabledIf.Root, clauses);
            foreach (ExpressionNode clause in clauses)
            {
                if (!clause.Evaluate(context, functions).IsTruthy)
                {
                    lines.Add(Render(clause, text));
                }
            }
            return lines;
        }

        private static void CollectAnd(ExpressionNode node, List<ExpressionNode> clauses)
        {
            switch (Unwrap(node))
            {
                case AndNode and:
                    CollectAnd(and.Left, clauses);
                    CollectAnd(and.Right, clauses);
                    break;
                default:
                    clauses.Add(node);
                    break;
            }
        }

        private static void CollectOr(ExpressionNode node, List<ExpressionNode> alternatives)
        {
            switch (Unwrap(node))
            {
                case OrNode or:
                    CollectOr(or.Left, alternatives);
                    CollectOr(or.Right, alternatives);
                    break;
                default:
                    alternatives.Add(node);
                    break;
            }
        }

        private static ExpressionNode Unwrap(ExpressionNode node)
        {
            while (node is GroupNode group)
            {
                node = group.Inner;
            }
            return node;
        }

        private static string Render(ExpressionNode node, IDialogTextResolver text)
        {
            switch (Unwrap(node))
            {
                case CompareNode compare when Unwrap(compare.Left) is VariableNode variable:
                    return $"{Name(variable.Name, text)} {Symbol(compare.Op)} {compare.Right.Source}";
                case VariableNode variable:
                    return Name(variable.Name, text);
                case CallNode call:
                    return string.IsNullOrEmpty(call.Function.Hint) ? call.Source : call.Function.Hint;
                case OrNode or:
                    var alternatives = new List<ExpressionNode>();
                    CollectOr(or, alternatives);
                    var parts = new List<string>();
                    foreach (ExpressionNode alternative in alternatives)
                    {
                        parts.Add(Render(alternative, text));
                    }
                    return "одно из: " + string.Join(" / ", parts);
                default:
                    return node.Source;
            }
        }

        private static string Name(string variable, IDialogTextResolver text) => text.Resolve(VariableKeyPrefix + variable, variable);

        private static string Symbol(CompareOp op) => op switch
        {
            CompareOp.Eq => "=",
            CompareOp.Ne => "≠",
            CompareOp.Lt => "<",
            CompareOp.Le => "≤",
            CompareOp.Gt => ">",
            _ => "≥",
        };
    }
}
