namespace MIDIFrogs.DialogSystem.Core
{
    public interface IDialogContext
    {
        T GetValue<T>(string key);

        void SetValue<T>(string key, T value);

        bool TryGetValue<T>(string key, out T value);
    }

    public interface IDialogCondition
    {
        bool Evaluate(IDialogContext context);
    }

    public interface IDialogAction
    {
        void Execute(IDialogContext context);
    }
}
