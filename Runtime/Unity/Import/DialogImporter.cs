using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Integration;
using MIDIFrogs.DialogSystem.Core;

namespace MIDIFrogs.DialogSystem.Unity.Conversion
{
    public static class DialogImporter
    {
        public static Dialog Import(DialogAsset asset)
        {
            var nodeCache = new Dictionary<DialogAsset.Node, DialogNode>();

            var startNode = ConvertNode(asset, asset.StartNode, nodeCache);

            return new Dialog
            {
                StartNode = startNode
            };
        }

        private static DialogNode ConvertNode(
            DialogAsset dialogAsset,
            DialogAsset.Node asset,
            Dictionary<DialogAsset.Node, DialogNode> cache)
        {
            if (asset == null)
                return null;

            if (cache.TryGetValue(asset, out var existing))
                return existing;

            var node = new DialogNode
            {
                Text = new StyledText
                {
                    Content = asset.Text,
                    Style = asset.FontStyle,
                },
                Author = ConvertAuthor(asset),
                BackgroundImage = new SpriteBackgroundImage(asset.FrameSplash),
                Voice = new AudioClipDialogVoiceover(asset.Voice),
                Choices = new List<DialogChoice>()
            };

            cache[asset] = node;

            if (asset.Choices == null)
            {
                node.Choices = new List<DialogChoice>();
                return node;
            }

            node.Choices = asset.Choices
                .Select(choice => ConvertChoice(dialogAsset, choice, cache))
                .ToList();

            return node;
        }

        private static DialogAuthor ConvertAuthor(DialogAsset.Node asset)
        {
            return asset.Author == null ?
                null :
                new DialogAuthor
                {
                    Id = asset.Author.Id,
                    Name = asset.Author.DisplayName,
                };
        }

        private static DialogChoice ConvertChoice(
            DialogAsset dialogAsset,
            DialogAsset.Choice choice,
            Dictionary<DialogAsset.Node, DialogNode> cache)
        {
            return new DialogChoice
            {
                Text = choice.Text,
                Conditions = choice.Conditions.Select(c => c.Create()).ToList(),
                Actions = choice.Actions.Select(a => a.Create()).ToList(),
                NextNode = ConvertNode(dialogAsset, dialogAsset.GetNodeByGUID(choice.NextNodeGuid), cache)
            };
        }
    }
}
