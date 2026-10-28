using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    internal enum TokenKind
    {
        Identifier,
        Number,
        String,
        True,
        False,
        LeftParen,
        RightParen,
        Comma,
        Semicolon,
        Not,
        And,
        Or,
        Eq,
        Ne,
        Lt,
        Le,
        Gt,
        Ge,
        Assign,
        PlusAssign,
        MinusAssign,
        End,
    }

    internal readonly struct Token
    {
        public Token(TokenKind kind, int position, int length, string text = null, double number = 0d)
        {
            Kind = kind;
            Position = position;
            Length = length;
            Text = text;
            Number = number;
        }

        public TokenKind Kind { get; }

        public int Position { get; }

        public int Length { get; }

        public string Text { get; }

        public double Number { get; }
    }

    internal sealed class ExpressionParseException : System.Exception
    {
        public ExpressionParseException(int position, int length, string message) : base(message)
        {
            Position = position;
            Length = length;
        }

        public int Position { get; }

        public int Length { get; }
    }

    internal static class ExpressionLexer
    {
        public static List<Token> Tokenize(string source)
        {
            var tokens = new List<Token>();
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_' || source[i] == '.'))
                    {
                        i++;
                    }
                    string word = source.Substring(start, i - start);
                    TokenKind kind = word switch
                    {
                        "true" => TokenKind.True,
                        "false" => TokenKind.False,
                        _ => TokenKind.Identifier,
                    };
                    tokens.Add(new Token(kind, start, i - start, word));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < source.Length && char.IsDigit(source[i]))
                    {
                        i++;
                    }
                    if (i + 1 < source.Length && source[i] == '.' && char.IsDigit(source[i + 1]))
                    {
                        i++;
                        while (i < source.Length && char.IsDigit(source[i]))
                        {
                            i++;
                        }
                    }
                    string text = source.Substring(start, i - start);
                    tokens.Add(new Token(TokenKind.Number, start, i - start, text, double.Parse(text, CultureInfo.InvariantCulture)));
                    continue;
                }

                if (c == '"')
                {
                    int start = i++;
                    var builder = new StringBuilder();
                    bool closed = false;
                    while (i < source.Length)
                    {
                        char s = source[i++];
                        if (s == '"')
                        {
                            closed = true;
                            break;
                        }
                        if (s == '\\' && i < source.Length && (source[i] == '"' || source[i] == '\\'))
                        {
                            s = source[i++];
                        }
                        builder.Append(s);
                    }
                    if (!closed)
                    {
                        throw new ExpressionParseException(start, source.Length - start, "The string is not closed with \".");
                    }
                    tokens.Add(new Token(TokenKind.String, start, i - start, builder.ToString()));
                    continue;
                }

                char next = i + 1 < source.Length ? source[i + 1] : '\0';
                switch (c)
                {
                    case '(':
                        tokens.Add(new Token(TokenKind.LeftParen, i++, 1));
                        continue;
                    case ')':
                        tokens.Add(new Token(TokenKind.RightParen, i++, 1));
                        continue;
                    case ',':
                        tokens.Add(new Token(TokenKind.Comma, i++, 1));
                        continue;
                    case ';':
                        tokens.Add(new Token(TokenKind.Semicolon, i++, 1));
                        continue;
                    case '!' when next == '=':
                        tokens.Add(new Token(TokenKind.Ne, i, 2));
                        i += 2;
                        continue;
                    case '!':
                        tokens.Add(new Token(TokenKind.Not, i++, 1));
                        continue;
                    case '&' when next == '&':
                        tokens.Add(new Token(TokenKind.And, i, 2));
                        i += 2;
                        continue;
                    case '|' when next == '|':
                        tokens.Add(new Token(TokenKind.Or, i, 2));
                        i += 2;
                        continue;
                    case '=' when next == '=':
                        tokens.Add(new Token(TokenKind.Eq, i, 2));
                        i += 2;
                        continue;
                    case '=':
                        tokens.Add(new Token(TokenKind.Assign, i++, 1));
                        continue;
                    case '<' when next == '=':
                        tokens.Add(new Token(TokenKind.Le, i, 2));
                        i += 2;
                        continue;
                    case '<':
                        tokens.Add(new Token(TokenKind.Lt, i++, 1));
                        continue;
                    case '>' when next == '=':
                        tokens.Add(new Token(TokenKind.Ge, i, 2));
                        i += 2;
                        continue;
                    case '>':
                        tokens.Add(new Token(TokenKind.Gt, i++, 1));
                        continue;
                    case '+' when next == '=':
                        tokens.Add(new Token(TokenKind.PlusAssign, i, 2));
                        i += 2;
                        continue;
                    case '-' when next == '=':
                        tokens.Add(new Token(TokenKind.MinusAssign, i, 2));
                        i += 2;
                        continue;
                }

                throw new ExpressionParseException(i, 1, $"Unexpected character '{c}'.");
            }

            tokens.Add(new Token(TokenKind.End, source.Length, 0));
            return tokens;
        }
    }
}
