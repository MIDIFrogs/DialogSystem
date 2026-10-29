using System;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.UI;
using MIDIFrogs.DialogSystem.Unity.Conversion;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Integration;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Samples
{
    /// <summary>
    /// Plays the demo dialog once. The context is the dialog's own state: the caller puts game values in before
    /// (here Gold) and reads the results after (Gold, Hired).
    /// </summary>
    public class SampleDialogRunner : MonoBehaviour
    {
        [SerializeField] private DialogAsset dialog;
        [SerializeField] private DialogRootView rootView;
        [Tooltip("Set to 10 or more to unlock the paid answer.")]
        [SerializeField] private float gold = 5f;

        private async void Start()
        {
            var service = new DialogService(rootView);
            var context = new DialogContext();
            context.SetValue("Gold", gold);
            try
            {
                await service.Play(DialogImporter.Import(dialog, service.Functions), context, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            context.TryGetValue("Gold", out object goldLeft);
            context.TryGetValue("Hired", out object hired);
            Debug.Log($"Dialog finished. Gold: {goldLeft}, hired: {hired ?? false}");
        }
    }
}
