using System;
using System.Collections.Generic;

using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Metadata
{
    [Serializable]
    public class LineNodeData
    {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public string GroupID { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
    }
}