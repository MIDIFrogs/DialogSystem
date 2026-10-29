namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>A parse error: the character range it covers and what is wrong.</summary>
    public sealed class ExpressionError
    {
        public ExpressionError(int position, int length, string message)
        {
            Position = position;
            Length = length;
            Message = message;
        }

        public int Position { get; }

        public int Length { get; }

        public string Message { get; }

        public override string ToString() => $"{Message} (at {Position})";
    }
}
