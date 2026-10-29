using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    internal abstract class ScriptStatement
    {
        public abstract void Execute(IDialogContext context, DialogFunctions functions);
    }

    internal sealed class AssignStatement : ScriptStatement
    {
        private readonly string name;
        private readonly TokenKind op;
        private readonly ExpressionNode value;

        public AssignStatement(string name, TokenKind op, ExpressionNode value)
        {
            this.name = name;
            this.op = op;
            this.value = value;
        }

        public override void Execute(IDialogContext context, DialogFunctions functions)
        {
            DialogValue right = value.Evaluate(context, functions);
            if (op == TokenKind.Assign)
            {
                context.SetValue(name, right.ToObject());
                return;
            }

            DialogValue left = context.TryGetValue(name, out object current) ? DialogValue.From(current) : DialogValue.Unset;
            if (left.Kind == DialogValueKind.Unset)
            {
                left = DialogValue.DefaultOf(right.Kind);
            }

            if (left.Kind == DialogValueKind.Number && right.Kind == DialogValueKind.Number)
            {
                double result = op == TokenKind.PlusAssign ? left.Number + right.Number : left.Number - right.Number;
                context.SetValue(name, result);
            }
            else if (op == TokenKind.PlusAssign && left.Kind == DialogValueKind.String)
            {
                context.SetValue(name, left.String + (right.Kind == DialogValueKind.String ? right.String : right.ToString()));
            }
        }
    }

    internal sealed class CallStatement : ScriptStatement
    {
        private readonly CallNode call;

        public CallStatement(CallNode call)
        {
            this.call = call;
        }

        public override void Execute(IDialogContext context, DialogFunctions functions) => call.Evaluate(context, functions);
    }

    /// <summary>Recursive descent: || &lt; &amp;&amp; &lt; ! &lt; comparison &lt; primary.</summary>
    internal sealed class ExpressionParser
    {
        private readonly string source;
        private readonly DialogFunctions functions;
        private readonly List<Token> tokens;
        private int index;

        public ExpressionParser(string source, DialogFunctions functions)
        {
            this.source = source;
            this.functions = functions ?? new DialogFunctions();
            tokens = ExpressionLexer.Tokenize(source);
        }

        private Token Current => tokens[index];

        public ExpressionNode ParseExpression()
        {
            ExpressionNode node = ParseOr();
            Expect(TokenKind.End, "Unexpected text after the expression.");
            return node;
        }

        public List<ScriptStatement> ParseScript()
        {
            var statements = new List<ScriptStatement>();
            while (Current.Kind != TokenKind.End)
            {
                if (Current.Kind == TokenKind.Semicolon)
                {
                    index++;
                    continue;
                }

                Token name = Expect(TokenKind.Identifier, "Expected a variable or a function call.");
                if (Current.Kind == TokenKind.LeftParen)
                {
                    statements.Add(new CallStatement(ParseCall(name)));
                }
                else if (Current.Kind is TokenKind.Assign or TokenKind.PlusAssign or TokenKind.MinusAssign)
                {
                    TokenKind op = Current.Kind;
                    index++;
                    statements.Add(new AssignStatement(name.Text, op, ParseOr()));
                }
                else
                {
                    throw Error(Current, "Expected =, += or -= after the variable name.");
                }

                if (Current.Kind != TokenKind.End)
                {
                    Expect(TokenKind.Semicolon, "Expected ; between statements.");
                }
            }
            return statements;
        }

        private ExpressionNode ParseOr()
        {
            ExpressionNode left = ParseAnd();
            while (Current.Kind == TokenKind.Or)
            {
                index++;
                ExpressionNode right = ParseAnd();
                left = new OrNode(left, right, left.Position, End(right) - left.Position, Slice(left, right));
            }
            return left;
        }

        private ExpressionNode ParseAnd()
        {
            ExpressionNode left = ParseUnary();
            while (Current.Kind == TokenKind.And)
            {
                index++;
                ExpressionNode right = ParseUnary();
                left = new AndNode(left, right, left.Position, End(right) - left.Position, Slice(left, right));
            }
            return left;
        }

        private ExpressionNode ParseUnary()
        {
            if (Current.Kind != TokenKind.Not)
            {
                return ParseComparison();
            }

            Token not = Current;
            index++;
            ExpressionNode operand = ParseUnary();
            int length = End(operand) - not.Position;
            return new NotNode(operand, not.Position, length, source.Substring(not.Position, length));
        }

        private ExpressionNode ParseComparison()
        {
            ExpressionNode left = ParsePrimary();
            CompareOp? op = Current.Kind switch
            {
                TokenKind.Eq => CompareOp.Eq,
                TokenKind.Ne => CompareOp.Ne,
                TokenKind.Lt => CompareOp.Lt,
                TokenKind.Le => CompareOp.Le,
                TokenKind.Gt => CompareOp.Gt,
                TokenKind.Ge => CompareOp.Ge,
                _ => null,
            };
            if (op == null)
            {
                return left;
            }

            index++;
            ExpressionNode right = ParsePrimary();
            return new CompareNode(op.Value, left, right, left.Position, End(right) - left.Position, Slice(left, right));
        }

        private ExpressionNode ParsePrimary()
        {
            Token token = Current;
            switch (token.Kind)
            {
                case TokenKind.Number:
                    index++;
                    return Literal(DialogValue.FromNumber(token.Number), token);
                case TokenKind.String:
                    index++;
                    return Literal(DialogValue.FromString(token.Text), token);
                case TokenKind.True:
                case TokenKind.False:
                    index++;
                    return Literal(DialogValue.FromBool(token.Kind == TokenKind.True), token);
                case TokenKind.Identifier:
                    index++;
                    return Current.Kind == TokenKind.LeftParen
                        ? ParseCall(token)
                        : new VariableNode(token.Text, token.Position, token.Length, token.Text);
                case TokenKind.LeftParen:
                    index++;
                    ExpressionNode inner = ParseOr();
                    Token close = Expect(TokenKind.RightParen, "Expected ')'.");
                    int length = close.Position + 1 - token.Position;
                    return new GroupNode(inner, token.Position, length, source.Substring(token.Position, length));
                case TokenKind.End:
                    throw Error(token, "The expression ends too early.");
                default:
                    throw Error(token, "Expected a value, a variable or '('.");
            }
        }

        private CallNode ParseCall(Token name)
        {
            if (!functions.TryGet(name.Text, out DialogFunction function))
            {
                throw new ExpressionParseException(name.Position, name.Length, $"Unknown function '{name.Text}'.");
            }

            Expect(TokenKind.LeftParen, "Expected '('.");
            var arguments = new List<ExpressionNode>();
            if (Current.Kind != TokenKind.RightParen)
            {
                arguments.Add(ParseOr());
                while (Current.Kind == TokenKind.Comma)
                {
                    index++;
                    arguments.Add(ParseOr());
                }
            }
            Token close = Expect(TokenKind.RightParen, "Expected ')' after the arguments.");
            int length = close.Position + 1 - name.Position;
            if (arguments.Count != function.ArgCount)
            {
                throw new ExpressionParseException(name.Position, length,
                    $"{function.Name} takes {function.ArgCount} argument(s), got {arguments.Count}.");
            }
            return new CallNode(function, arguments, name.Position, length, source.Substring(name.Position, length));
        }

        private LiteralNode Literal(DialogValue value, Token token) =>
            new(value, token.Position, token.Length, source.Substring(token.Position, token.Length));

        private Token Expect(TokenKind kind, string message)
        {
            Token token = Current;
            if (token.Kind != kind)
            {
                throw Error(token, message);
            }
            index++;
            return token;
        }

        private static ExpressionParseException Error(Token token, string message) =>
            new(token.Position, System.Math.Max(token.Length, 1), message);

        private static int End(ExpressionNode node) => node.Position + node.Length;

        private string Slice(ExpressionNode left, ExpressionNode right) =>
            source.Substring(left.Position, End(right) - left.Position);
    }
}
