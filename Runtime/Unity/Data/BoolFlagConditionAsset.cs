using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    [CreateAssetMenu(menuName = "Dialog System/Conditions/Bool Flag")]
    public class BoolFlagConditionAsset : ConditionAsset
    {
        public string Id;
        public bool Expected;

        public override IDialogCondition Create()
        {
            return new BoolFlagCondition
            {
                Id = Id,
                Expected = Expected
            };
        }
    }
}
