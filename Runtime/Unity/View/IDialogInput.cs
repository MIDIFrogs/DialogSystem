namespace MIDIFrogs.DialogSystem.Unity.View
{
    /// <summary>The "continue" input (Submit, click, gamepad button) a dialog view polls each frame.</summary>
    public interface IDialogInput
    {
        bool AdvancePressedThisFrame();
    }
}
