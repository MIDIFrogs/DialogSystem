using System.Text.RegularExpressions;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Unity.Conversion;
using MIDIFrogs.DialogSystem.Unity.Data;
using MIDIFrogs.DialogSystem.Unity.Integration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class DialogImporterTests
    {
        private DialogAsset asset;
        private DialogFunctions functions;

        [SetUp]
        public void SetUp()
        {
            asset = ScriptableObject.CreateInstance<DialogAsset>();
            asset.name = "Meeting";
            asset.Id = "meeting";
            functions = new DialogFunctions();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(asset);
        }

        private DialogAsset.Line AddLine(string id, string text)
        {
            var line = new DialogAsset.Line { Id = id, Text = text };
            asset.Lines.Add(line);
            return line;
        }

        [Test]
        public void ImportFollowsStartAndNext()
        {
            AddLine("a", "Hello").NextId = "b";
            AddLine("b", "Bye");
            asset.StartNodeId = "a";

            Dialog dialog = DialogImporter.Import(asset, functions);

            Assert.AreEqual("Hello", dialog.StartNode.Text.Content);
            Assert.AreEqual("Bye", dialog.StartNode.Next.Text.Content);
            Assert.IsNull(dialog.StartNode.Next.Next);
        }

        [Test]
        public void ImportHandlesCycles()
        {
            AddLine("a", "A").NextId = "b";
            DialogAsset.Line b = AddLine("b", "B");
            b.Answers.Add(new DialogAsset.Answer { Id = "again", Text = "Again", TargetId = "a" });
            asset.StartNodeId = "a";

            Dialog dialog = DialogImporter.Import(asset, functions);

            DialogNode a = dialog.StartNode;
            Assert.AreSame(a, a.Next.Choices[0].Target);
        }

        [Test]
        public void MissingStartWarnsAndEndsDialog()
        {
            AddLine("a", "A");
            LogAssert.Expect(LogType.Warning, new Regex("Start"));

            Dialog dialog = DialogImporter.Import(asset, functions);

            Assert.IsNull(dialog.StartNode);
        }

        [Test]
        public void BrokenExpressionWarnsOnceAndIsFalse()
        {
            DialogAsset.Line a = AddLine("a", "A");
            a.Answers.Add(new DialogAsset.Answer { Id = "x", Text = "X", VisibleIf = "Gold >", Actions = "Gold +=" });
            asset.StartNodeId = "a";
            LogAssert.Expect(LogType.Warning, new Regex("Meeting/a.*visible"));
            LogAssert.Expect(LogType.Warning, new Regex("Meeting/a.*actions"));

            Dialog dialog = DialogImporter.Import(asset, functions);

            DialogChoice choice = dialog.StartNode.Choices[0];
            Assert.IsFalse(choice.VisibleIf.Evaluate(new DialogContext()));
            Assert.AreEqual(0, choice.Actions.Count);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void KeysAreStable()
        {
            DialogAsset.Line a = AddLine("a", "A");
            a.Answers.Add(new DialogAsset.Answer { Id = "x", Text = "X", Hint = "H" });
            asset.StartNodeId = "a";
            asset.name = "Renamed";

            Dialog dialog = DialogImporter.Import(asset, functions);

            Assert.AreEqual("dialog.meeting.a", dialog.StartNode.Text.Key);
            Assert.AreEqual("dialog.meeting.a.x", dialog.StartNode.Choices[0].TextKey);
            Assert.AreEqual("dialog.meeting.a.x.hint", dialog.StartNode.Choices[0].HintKey);
            Assert.AreEqual(DialogKeys.Line("meeting", "a"), dialog.StartNode.Text.Key);
        }

        [Test]
        public void AuthorIsCopiedFromAsset()
        {
            var author = ScriptableObject.CreateInstance<DialogAuthorAsset>();
            author.Id = "smith";
            author.DisplayName = "Кузнец";
            AddLine("a", "A").Author = author;
            asset.StartNodeId = "a";

            Dialog dialog = DialogImporter.Import(asset, functions);

            Assert.AreEqual("Кузнец", dialog.StartNode.Author.Name);
            Assert.AreEqual("dialog.author.smith", dialog.StartNode.Author.NameKey);
            Assert.AreSame(author, dialog.StartNode.Author.Source);
            Object.DestroyImmediate(author);
        }
    }
}
