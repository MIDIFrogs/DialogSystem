using System;
using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Unity;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Metadata
{
    [Serializable]
    public class DialogResponseData
    {
        [field: SerializeField] public string Text { get; set; }
        [field: SerializeField] public string NodeID { get; set; }
        [field: SerializeField] public List<ConditionAsset> Conditions { get; set; }
        [field: SerializeField] public List<ActionAsset> Actions { get; set; }
    }
}