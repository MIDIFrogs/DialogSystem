namespace MIDIFrogs.DialogSystem.Core
{
    public class BoolFlagCondition : IDialogCondition
    {
        public string Id;
        public bool Expected;

        public bool Evaluate(IDialogContext context)
            => context.TryGetValue(Id, out object value) && value is bool flag && flag == Expected;
    }

    public class SetFlagAction : IDialogAction
    {
        public string Id;
        public bool Value;

        public void Execute(IDialogContext context) => context.SetValue(Id, Value);
    }
}
