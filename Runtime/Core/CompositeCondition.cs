using System.Collections.Generic;
using System.Linq;

namespace MIDIFrogs.DialogSystem.Core
{
    public class CompositeCondition : IDialogCondition
    {
        public enum Operation
        {
            And,
            Or,
            Not
        }

        public Operation Op;
        public List<IDialogCondition> Conditions = new();

        public bool Evaluate(IDialogContext context)
        {
            return Op switch
            {
                Operation.And => Conditions.All(c => c.Evaluate(context)),
                Operation.Or => Conditions.Any(c => c.Evaluate(context)),
                Operation.Not => Conditions.Count > 0 && !Conditions[0].Evaluate(context),
                _ => false
            };
        }
    }
}
