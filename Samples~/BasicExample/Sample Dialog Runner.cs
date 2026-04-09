using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.UI;
using MIDIFrogs.DialogSystem.Unity.Conversion;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Integration;
using UnityEngine;

public class SampleDialogRunner : MonoBehaviour
{
    [SerializeField] private DialogAsset dialog;
    [SerializeField] private DialogRootView rootView;
    [SerializeField] private ConditionAsset sampleConditionAsset;

    private readonly DialogContext context = new();

    async void Start()
    {
        var imported = DialogImporter.Import(dialog);
        var dialogService = new DialogService(rootView, context);

        await dialogService.Play(imported);
        Debug.Log("Dialog completed!");

        if (sampleConditionAsset != null)
        {
            var condition = sampleConditionAsset.Create();
            if (condition.Evaluate(context))
                Debug.Log("Condition satisfied after playing dialog.");
            else
                Debug.Log("Condition not satisfied after playing dialog.");
        }
    }
}
