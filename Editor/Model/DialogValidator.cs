using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Unity.Data;

namespace MIDIFrogs.DialogSystem.Editor.Model
{
    /// <summary>Finds authoring problems in a dialog asset.</summary>
    public static class DialogValidator
    {
        public static IReadOnlyList<DialogWarning> Validate(DialogAsset dialog, DialogFunctions functions)
        {
            var warnings = new List<DialogWarning>();
            functions ??= new DialogFunctions();

            if (string.IsNullOrEmpty(dialog.StartNodeId))
            {
                warnings.Add(new DialogWarning(null, null, nameof(DialogAsset.StartNodeId),
                    "Старт не связан с репликой: диалог сразу закончится."));
            }
            else if (dialog.FindLine(dialog.StartNodeId) == null)
            {
                warnings.Add(new DialogWarning(null, null, nameof(DialogAsset.StartNodeId),
                    "Старт ведёт к удалённой реплике."));
            }

            HashSet<string> reachable = Reachable(dialog);
            foreach (DialogAsset.Line line in dialog.Lines)
            {
                if (!reachable.Contains(line.Id))
                {
                    warnings.Add(new DialogWarning(line.Id, null, null, "Реплика недостижима от Старта."));
                }

                if (!string.IsNullOrEmpty(line.NextId) && dialog.FindLine(line.NextId) == null)
                {
                    warnings.Add(new DialogWarning(line.Id, null, nameof(DialogAsset.Line.NextId),
                        "«Далее» ведёт к удалённой реплике."));
                }

                bool allConditional = line.Answers.Count > 0;
                foreach (DialogAsset.Answer answer in line.Answers)
                {
                    allConditional &= !string.IsNullOrWhiteSpace(answer.VisibleIf) || !string.IsNullOrWhiteSpace(answer.EnabledIf);
                    CheckAnswer(dialog, line, answer, functions, warnings);
                }

                if (allConditional && string.IsNullOrEmpty(line.NextId))
                {
                    warnings.Add(new DialogWarning(line.Id, null, nameof(DialogAsset.Line.NextId),
                        "Все ответы с условиями, а «Далее» не задано: если ни один не доступен, диалог закончится здесь."));
                }
            }

            var titles = new HashSet<string>();
            foreach (DialogAsset.Group group in dialog.Groups)
            {
                if (!string.IsNullOrEmpty(group.Title) && !titles.Add(group.Title))
                {
                    warnings.Add(new DialogWarning(null, null, nameof(DialogAsset.Group.Title),
                        $"Несколько групп называются «{group.Title}»."));
                }
            }

            return warnings;
        }

        private static void CheckAnswer(DialogAsset dialog, DialogAsset.Line line, DialogAsset.Answer answer,
            DialogFunctions functions, List<DialogWarning> warnings)
        {
            if (!string.IsNullOrEmpty(answer.TargetId) && dialog.FindLine(answer.TargetId) == null)
            {
                warnings.Add(new DialogWarning(line.Id, answer.Id, nameof(DialogAsset.Answer.TargetId),
                    "Ответ ведёт к удалённой реплике."));
            }

            CheckCondition(line, answer, nameof(DialogAsset.Answer.VisibleIf), answer.VisibleIf, functions, warnings);
            CheckCondition(line, answer, nameof(DialogAsset.Answer.EnabledIf), answer.EnabledIf, functions, warnings);
            if (DialogScript.Parse(answer.Actions, functions, out ExpressionError error) == null)
            {
                warnings.Add(new DialogWarning(line.Id, answer.Id, nameof(DialogAsset.Answer.Actions), error.Message, error));
            }
        }

        private static void CheckCondition(DialogAsset.Line line, DialogAsset.Answer answer, string field, string source,
            DialogFunctions functions, List<DialogWarning> warnings)
        {
            if (DialogExpression.Parse(source, functions, out ExpressionError error) == null)
            {
                warnings.Add(new DialogWarning(line.Id, answer.Id, field, error.Message, error));
            }
        }

        private static HashSet<string> Reachable(DialogAsset dialog)
        {
            var seen = new HashSet<string>();
            var queue = new Queue<string>();
            Visit(dialog.StartNodeId);
            while (queue.Count > 0)
            {
                DialogAsset.Line line = dialog.FindLine(queue.Dequeue());
                Visit(line.NextId);
                foreach (DialogAsset.Answer answer in line.Answers)
                {
                    Visit(answer.TargetId);
                }
            }
            return seen;

            void Visit(string id)
            {
                if (dialog.FindLine(id) != null && seen.Add(id))
                {
                    queue.Enqueue(id);
                }
            }
        }
    }
}
