using System.Collections.Generic;
using System.Linq;

namespace MIDIFrogs.DialogSystem.Core
{
    public class DialogRunner
    {
        private readonly IDialogContext context;

        public DialogNode CurrentNode { get; private set; }

        public DialogRunner(IDialogContext context)
        {
            this.context = context;
        }

        public void Start(Dialog dialog)
        {
            CurrentNode = dialog.StartNode;
        }

        public IEnumerable<DialogChoice> GetAvailableChoices()
        {
            if (CurrentNode == null)
                return Enumerable.Empty<DialogChoice>();

            return CurrentNode.Choices
                .Where(c => c.Conditions.All(cond => cond.Evaluate(context)));
        }

        public void Choose(DialogChoice choice)
        {
            if (choice == null)
                return;

            foreach (var action in choice.Actions)
            {
                action.Execute(context);
            }

            CurrentNode = choice.NextNode;
        }

        public bool IsFinished => CurrentNode == null;
    }
}