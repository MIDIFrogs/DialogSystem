using System;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Metadata
{
    [Serializable]
    public class DialogGroupData
    {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
    }
}