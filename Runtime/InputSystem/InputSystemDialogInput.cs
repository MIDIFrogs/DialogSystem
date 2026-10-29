using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MIDIFrogs.DialogSystem.InputSystem
{
    /// <summary>
    /// "Continue" from an Input System action, e.g. UI/Submit. Mouse clicks come from the view's continue prompt,
    /// so no click action is needed (one would advance twice per click).
    /// </summary>
    public class InputSystemDialogInput : MonoBehaviour, IDialogInput
    {
        [SerializeField] private InputActionReference advance;

        public bool AdvancePressedThisFrame() => Pressed(advance);

        private void OnEnable()
        {
            Enable(advance);
        }

        private static bool Pressed(InputActionReference reference) =>
            reference != null && reference.action != null && reference.action.WasPressedThisFrame();

        private static void Enable(InputActionReference reference)
        {
            if (reference != null && reference.action != null && !reference.action.enabled)
            {
                reference.action.Enable();
            }
        }
    }
}
