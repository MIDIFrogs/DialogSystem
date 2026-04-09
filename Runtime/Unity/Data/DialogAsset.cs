using System;
using System.Collections.Generic;
using UnityEngine;

using FontStyle = MIDIFrogs.DialogSystem.Core.FontStyle;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    [CreateAssetMenu(menuName = "Dialog System/Dialog")]
    public class DialogAsset : ScriptableObject
    {
        [Serializable]
        public class Node
        {
            [HideInInspector]
            [SerializeField]
            private string _guid = Guid.NewGuid().ToString();
            public string GUID => _guid;

            [TextArea] public string Text;
            public DialogAuthorAsset Author;
            public AudioClip Voice;
            public string GroupId;
            public Sprite FrameSplash;
            public FontStyle FontStyle;
            public List<Choice> Choices = new();
        }

        [Serializable]
        public class Choice
        {
            public string Text;
            public List<ConditionAsset> Conditions = new();
            public List<ActionAsset> Actions = new();
            public string NextNodeGuid;
        }

        public List<Node> Nodes = new();

        public Node StartNode => Nodes.Count > 0 ? Nodes[0] : null;

        public Node GetNodeByGUID(string guid)
        {
            return Nodes.Find(n => n.GUID == guid);
        }
    }
}
