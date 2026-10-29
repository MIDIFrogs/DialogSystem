using System;
using System.Collections.Generic;
using UnityEngine;
using FontStyle = MIDIFrogs.DialogSystem.Core.FontStyle;

namespace MIDIFrogs.DialogSystem.Unity.Data
{
    /// <summary>
    /// A dialog: a Start link, lines with a Next link and answers, and the editor layout. Ids are strings so
    /// links and localization keys survive edits and renames.
    /// </summary>
    [CreateAssetMenu(menuName = "Dialog System/Dialog")]
    public class DialogAsset : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            public string Id = Guid.NewGuid().ToString("N");
            [TextArea] public string Text = string.Empty;
            public DialogAuthorAsset Author;
            [SerializeReference, DialogSound] public IDialogSound Voice;
            public Sprite Background;
            public FontStyle FontStyle;
            public bool OverrideColor;
            public Color Color = Color.white;
            public string NextId;
            public List<Answer> Answers = new();
            public Vector2 Position;
            public string GroupId;
        }

        [Serializable]
        public class Answer
        {
            public string Id = Guid.NewGuid().ToString("N");
            public string Text = string.Empty;
            public string VisibleIf = string.Empty;
            public string EnabledIf = string.Empty;
            public string Hint = string.Empty;
            public string Actions = string.Empty;
            public string TargetId;
        }

        [Serializable]
        public class Group
        {
            public string Id = Guid.NewGuid().ToString("N");
            public string Title = string.Empty;
            public Vector2 Position;
        }

        [Tooltip("Stable id for localization keys; set from the file name when the dialog is created.")]
        public string Id;
        public string StartNodeId;
        public Vector2 StartPosition;
        public List<Line> Lines = new();
        public List<Group> Groups = new();

        public Line FindLine(string id) => string.IsNullOrEmpty(id) ? null : Lines.Find(line => line.Id == id);

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString("N");
            }
        }
    }
}
