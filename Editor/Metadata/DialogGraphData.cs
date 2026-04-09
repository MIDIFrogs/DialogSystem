using System.Collections.Generic;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Metadata
{
    public class DialogGraphData : ScriptableObject
    {
        [field: SerializeField] public string FileName { get; set; }
        [field: SerializeField] public List<DialogGroupData> Groups { get; set; }
        [field: SerializeField] public List<LineNodeData> Lines { get; set; }

        public void Initialize(string fileName)
        {
            FileName = fileName;

            Groups = new List<DialogGroupData>();
            Lines = new List<LineNodeData>();
        }
    }
}