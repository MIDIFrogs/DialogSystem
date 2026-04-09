using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    [CreateAssetMenu(menuName = "Dialog System/Actions/Set Flag Action")]
    public class SetFlagActionAsset : ActionAsset
    {
        public string Id;
        public bool Value;

        public override IDialogAction Create()
        {
            return new SetFlagAction
            {
                Id = Id,
                Value = Value,
            };
        }
    }
}
