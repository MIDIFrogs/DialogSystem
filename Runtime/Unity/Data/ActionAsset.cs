using MIDIFrogs.DialogSystem.Core;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    public abstract class ActionAsset : ScriptableObject
    {
        public abstract IDialogAction Create();
    }
}
