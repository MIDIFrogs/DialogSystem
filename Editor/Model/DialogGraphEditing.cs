using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Unity.Data;
using UnityEditor;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>Graph edits on a dialog asset. Every edit records Undo on the asset.</summary>
    public static class DialogGraphEditing
    {
        public static DialogAsset.Line AddLine(DialogAsset dialog, Vector2 position, DialogAuthorAsset author)
        {
            Record(dialog, "Add line");
            var line = new DialogAsset.Line { Position = position, Author = author };
            dialog.Lines.Add(line);
            return line;
        }

        public static void SetStart(DialogAsset dialog, string lineId)
        {
            Record(dialog, "Link Start");
            dialog.StartNodeId = lineId;
        }

        public static void SetNext(DialogAsset dialog, string fromLineId, string toLineId)
        {
            DialogAsset.Line line = dialog.FindLine(fromLineId);
            if (line == null)
            {
                return;
            }

            Record(dialog, "Link Next");
            line.NextId = toLineId;
        }

        public static DialogAsset.Answer AddAnswer(DialogAsset dialog, string lineId)
        {
            DialogAsset.Line line = dialog.FindLine(lineId);
            if (line == null)
            {
                return null;
            }

            Record(dialog, "Add answer");
            var answer = new DialogAsset.Answer();
            line.Answers.Add(answer);
            return answer;
        }

        public static void RemoveAnswer(DialogAsset dialog, string lineId, string answerId)
        {
            DialogAsset.Line line = dialog.FindLine(lineId);
            if (line == null)
            {
                return;
            }

            Record(dialog, "Remove answer");
            line.Answers.RemoveAll(answer => answer.Id == answerId);
        }

        public static void SetAnswerTarget(DialogAsset dialog, string lineId, string answerId, string toLineId)
        {
            DialogAsset.Answer answer = dialog.FindLine(lineId)?.Answers.Find(a => a.Id == answerId);
            if (answer == null)
            {
                return;
            }

            Record(dialog, "Link answer");
            answer.TargetId = toLineId;
        }

        /// <summary>Removes the lines and clears every Start, Next and answer link that pointed at them.</summary>
        public static void RemoveLines(DialogAsset dialog, IEnumerable<string> lineIds)
        {
            var removed = new HashSet<string>(lineIds);
            if (removed.Count == 0)
            {
                return;
            }

            Record(dialog, "Remove lines");
            dialog.Lines.RemoveAll(line => removed.Contains(line.Id));
            if (dialog.StartNodeId != null && removed.Contains(dialog.StartNodeId))
            {
                dialog.StartNodeId = null;
            }

            foreach (DialogAsset.Line line in dialog.Lines)
            {
                if (line.NextId != null && removed.Contains(line.NextId))
                {
                    line.NextId = null;
                }

                foreach (DialogAsset.Answer answer in line.Answers)
                {
                    if (answer.TargetId != null && removed.Contains(answer.TargetId))
                    {
                        answer.TargetId = null;
                    }
                }
            }
        }

        public static void MoveLine(DialogAsset dialog, string lineId, Vector2 position)
        {
            DialogAsset.Line line = dialog.FindLine(lineId);
            if (line == null)
            {
                return;
            }

            Record(dialog, "Move line");
            line.Position = position;
        }

        public static DialogAsset.Group AddGroup(DialogAsset dialog, string title, Vector2 position)
        {
            Record(dialog, "Add group");
            var group = new DialogAsset.Group { Title = title ?? string.Empty, Position = position };
            dialog.Groups.Add(group);
            return group;
        }

        /// <summary>Puts the lines into the group; a null or empty group id takes them out of any group.</summary>
        public static void SetGroup(DialogAsset dialog, IEnumerable<string> lineIds, string groupId)
        {
            Record(dialog, "Group lines");
            foreach (string id in lineIds)
            {
                DialogAsset.Line line = dialog.FindLine(id);
                if (line != null)
                {
                    line.GroupId = groupId;
                }
            }
        }

        public static void RemoveGroup(DialogAsset dialog, string groupId)
        {
            Record(dialog, "Remove group");
            dialog.Groups.RemoveAll(group => group.Id == groupId);
            foreach (DialogAsset.Line line in dialog.Lines)
            {
                if (line.GroupId == groupId)
                {
                    line.GroupId = null;
                }
            }
        }

        /// <summary>Records Undo for an edit made outside this class (e.g. a text field).</summary>
        public static void Record(DialogAsset dialog, string action) => Undo.RecordObject(dialog, action);
    }
}
