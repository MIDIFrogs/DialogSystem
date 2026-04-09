using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    public abstract class ConditionAsset : ScriptableObject
    {
        public abstract IDialogCondition Create();
    }
}