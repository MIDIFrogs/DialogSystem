using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Integration;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Unity.Conversion
{
    /// <summary>
    /// Builds a runtime dialog from an asset. Expressions are parsed once; a broken one logs one warning and
    /// becomes false (actions: nothing). A missing Start ends the dialog at once.
    /// </summary>
    public static class DialogImporter
    {
        public static Dialog Import(DialogAsset asset, DialogFunctions functions)
        {
            functions ??= new DialogFunctions();
            var nodes = new Dictionary<string, DialogNode>();
            foreach (DialogAsset.Line line in asset.Lines)
            {
                if (line != null && !string.IsNullOrEmpty(line.Id) && !nodes.ContainsKey(line.Id))
                {
                    nodes[line.Id] = CreateNode(asset, line);
                }
            }

            foreach (DialogAsset.Line line in asset.Lines)
            {
                if (line == null || !nodes.TryGetValue(line.Id, out DialogNode node))
                {
                    continue;
                }

                node.Next = Find(nodes, line.NextId);
                foreach (DialogAsset.Answer answer in line.Answers)
                {
                    if (answer != null)
                    {
                        node.Choices.Add(CreateChoice(asset, line, answer, nodes, functions));
                    }
                }
            }

            DialogNode start = Find(nodes, asset.StartNodeId);
            if (start == null)
            {
                Debug.LogWarning($"[DialogSystem] {asset.name}: Start is not linked to a line, so the dialog ends at once.", asset);
            }
            return new Dialog { Id = asset.Id, StartNode = start };
        }

        private static DialogNode CreateNode(DialogAsset asset, DialogAsset.Line line) => new()
        {
            Id = line.Id,
            Text = new StyledText
            {
                Content = line.Text ?? string.Empty,
                Key = DialogKeys.Line(asset.Id, line.Id),
                Style = line.FontStyle,
                HasColor = line.OverrideColor,
                Color = line.Color,
            },
            Author = CreateAuthor(line.Author),
            Voice = line.Voice,
            BackgroundImage = new SpriteBackgroundImage(line.Background),
        };

        private static DialogAuthor CreateAuthor(DialogAuthorAsset author)
        {
            if (author == null)
            {
                return null;
            }

            return new DialogAuthor
            {
                Id = author.Id,
                Name = author.DisplayName,
                NameKey = DialogKeys.Author(author.Id),
                Source = author,
            };
        }

        private static DialogChoice CreateChoice(DialogAsset asset, DialogAsset.Line line, DialogAsset.Answer answer,
            Dictionary<string, DialogNode> nodes, DialogFunctions functions) => new()
        {
            Id = answer.Id,
            Text = answer.Text ?? string.Empty,
            TextKey = DialogKeys.Answer(asset.Id, line.Id, answer.Id),
            VisibleIf = Condition(asset, line, "visible-if", answer.VisibleIf, functions),
            EnabledIf = Condition(asset, line, "enabled-if", answer.EnabledIf, functions),
            Hint = answer.Hint,
            HintKey = DialogKeys.Hint(asset.Id, line.Id, answer.Id),
            Actions = Script(asset, line, answer.Actions, functions),
            Target = Find(nodes, answer.TargetId),
        };

        private static DialogExpression Condition(DialogAsset asset, DialogAsset.Line line, string field, string source,
            DialogFunctions functions)
        {
            DialogExpression expression = DialogExpression.Parse(source, functions, out ExpressionError error);
            if (expression != null)
            {
                return expression;
            }

            Debug.LogWarning($"[DialogSystem] {asset.name}/{line.Id}: {field} '{source}' does not parse ({error}); it counts as false.", asset);
            return DialogExpression.Constant(false);
        }

        private static DialogScript Script(DialogAsset asset, DialogAsset.Line line, string source, DialogFunctions functions)
        {
            DialogScript script = DialogScript.Parse(source, functions, out ExpressionError error);
            if (script != null)
            {
                return script;
            }

            Debug.LogWarning($"[DialogSystem] {asset.name}/{line.Id}: actions '{source}' do not parse ({error}); they are skipped.", asset);
            return DialogScript.Empty;
        }

        private static DialogNode Find(Dictionary<string, DialogNode> nodes, string id) =>
            !string.IsNullOrEmpty(id) && nodes.TryGetValue(id, out DialogNode node) ? node : null;
    }
}
