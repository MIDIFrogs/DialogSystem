using MIDIFrogs.DialogSystem.Unity.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MIDIFrogs.DialogSystem.InputSystem
{
    /// <summary>"Continue" from Input System actions, e.g. UI/Submit and UI/Click.</summary>
    public class InputSystemDialogInput : MonoBehaviour, IDialogInput
    {
        [SerializeField] private InputActionReference advance;
        [SerializeField] private InputActionReference click;

        public bool AdvancePressedThisFrame() => Pressed(advance) || Pressed(click);

        private void OnEnable()
        {
            Enable(advance);
            Enable(click);
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
