using System;
using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Core.Hints;

namespace MIDIFrogs.DialogSystem.Core
{
    /// <summary>
    /// Walks a dialog. On entering a line it evaluates the answers once: hidden ones are dropped, locked ones carry
    /// a hint. With no enabled answer the line continues along <see cref="DialogNode.Next"/> (or ends).
    /// </summary>
    public class DialogRunner
    {
        private static readonly IReadOnlyList<ChoiceState> NoChoices = Array.Empty<ChoiceState>();

        private readonly IDialogContext context;
        private readonly DialogFunctions functions;
        private readonly IDialogTextResolver text;
        private IReadOnlyList<ChoiceState> choices = NoChoices;

        public DialogRunner(IDialogContext context, DialogFunctions functions, IDialogTextResolver text)
        {
            this.context = context ?? new DialogContext();
            this.functions = functions ?? new DialogFunctions();
            this.text = text ?? SourceTextResolver.Instance;
        }

        public DialogNode CurrentNode { get; private set; }

        /// <summary>Visible answers of the current line, evaluated when the line was entered.</summary>
        public IReadOnlyList<ChoiceState> Choices => choices;

        /// <summary>True when at least one visible answer can be picked.</summary>
        public bool IsWaitingForChoice { get; private set; }

        public bool IsFinished => CurrentNode == null;

        public void Start(Dialog dialog) => Enter(dialog?.StartNode);

        /// <summary>Continues along Next (or ends the dialog).</summary>
        public void Advance() => Enter(CurrentNode?.Next);

        /// <summary>Picks a visible, enabled answer of the current line; anything else is ignored.</summary>
        public bool Choose(DialogChoice choice)
        {
            if (choice == null)
            {
                return false;
            }

            foreach (ChoiceState state in choices)
            {
                if (state.Choice == choice && state.Enabled)
                {
                    choice.Actions?.Execute(context);
                    Enter(choice.Target);
                    return true;
                }
            }
            return false;
        }

        private void Enter(DialogNode node)
        {
            CurrentNode = node;
            IsWaitingForChoice = false;
            if (node == null || node.Choices.Count == 0)
            {
                choices = NoChoices;
                return;
            }

            var states = new List<ChoiceState>();
            foreach (DialogChoice choice in node.Choices)
            {
                if (choice == null || !(choice.VisibleIf?.Evaluate(context) ?? true))
                {
                    continue;
                }

                bool enabled = choice.EnabledIf?.Evaluate(context) ?? true;
                IReadOnlyList<string> hint = enabled ? Array.Empty<string>() : Hint(choice);
                states.Add(new ChoiceState(choice, enabled, hint));
                IsWaitingForChoice |= enabled;
            }
            choices = states;
        }

        private IReadOnlyList<string> Hint(DialogChoice choice)
        {
            if (!string.IsNullOrEmpty(choice.Hint))
            {
                return new[] { text.Resolve(choice.HintKey, choice.Hint) };
            }
            return ConditionHints.Describe(choice.EnabledIf, context, functions, text);
        }
    }
}
