using System.Collections.Generic;

namespace MIDIFrogs.DialogSystem.Core.Expressions
{
    /// <summary>Actions of an answer: <c>Gold -= 10; QuestStarted = true; GiveItem("Sword")</c>.</summary>
    public sealed class DialogScript
    {
        private readonly List<ScriptStatement> statements;
        private readonly DialogFunctions functions;

        private DialogScript(string source, List<ScriptStatement> statements, DialogFunctions functions)
        {
            Source = source;
            this.statements = statements;
            this.functions = functions;
        }

        public static DialogScript Empty => new(string.Empty, new List<ScriptStatement>(), new DialogFunctions());

        public string Source { get; }

        public int Count => statements.Count;

        /// <summary>Returns null and sets <paramref name="error"/> when the source does not parse.</summary>
        public static DialogScript Parse(string source, DialogFunctions functions, out ExpressionError error)
        {
            error = null;
            source ??= string.Empty;
            functions ??= new DialogFunctions();
            try
            {
                return new DialogScript(source, new ExpressionParser(source, functions).ParseScript(), functions);
            }
            catch (ExpressionParseException exception)
            {
                error = new ExpressionError(exception.Position, exception.Length, exception.Message);
                return null;
            }
        }

        public void Execute(IDialogContext context)
        {
            foreach (ScriptStatement statement in statements)
            {
                statement.Execute(context, functions);
            }
        }
    }
}
