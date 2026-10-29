using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Editor.Model;
using MIDIFrogs.DialogSystem.Unity.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class EditorModelTests
    {
        private const string TempFolder = "Assets/DialogSystemTestTemp";

        private DialogAsset dialog;
        private DialogAuthorAsset author;
        private DialogFunctions functions;

        [SetUp]
        public void SetUp()
        {
            dialog = ScriptableObject.CreateInstance<DialogAsset>();
            dialog.Id = "meeting";
            author = ScriptableObject.CreateInstance<DialogAuthorAsset>();
            functions = new DialogFunctions();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(dialog);
            Object.DestroyImmediate(author);
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
        }

        private IReadOnlyList<DialogWarning> Validate() => DialogValidator.Validate(dialog, functions);

        [Test]
        public void AddLineUsesPaletteAuthor()
        {
            DialogAsset.Line line = DialogGraphEditing.AddLine(dialog, new Vector2(10, 20), author);

            Assert.AreSame(author, line.Author);
            Assert.AreEqual(new Vector2(10, 20), line.Position);
            Assert.AreEqual(0, line.Answers.Count);
            Assert.AreSame(line, dialog.FindLine(line.Id));
        }

        [Test]
        public void RemoveLinesClearsIncomingLinks()
        {
            DialogAsset.Line a = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogAsset.Line b = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogGraphEditing.SetStart(dialog, b.Id);
            DialogGraphEditing.SetNext(dialog, a.Id, b.Id);
            DialogAsset.Answer answer = DialogGraphEditing.AddAnswer(dialog, a.Id);
            DialogGraphEditing.SetAnswerTarget(dialog, a.Id, answer.Id, b.Id);

            DialogGraphEditing.RemoveLines(dialog, new[] { b.Id });

            Assert.IsNull(dialog.FindLine(b.Id));
            Assert.IsTrue(string.IsNullOrEmpty(a.NextId));
            Assert.IsTrue(string.IsNullOrEmpty(answer.TargetId));
            Assert.IsTrue(string.IsNullOrEmpty(dialog.StartNodeId));
        }

        [Test]
        public void GroupsHoldLines()
        {
            DialogAsset.Line a = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogAsset.Group group = DialogGraphEditing.AddGroup(dialog, "Встреча", Vector2.zero);

            DialogGraphEditing.SetGroup(dialog, new[] { a.Id }, group.Id);

            Assert.AreEqual(group.Id, a.GroupId);
        }

        [Test]
        public void ValidatorWarnsAboutUnlinkedStart()
        {
            DialogGraphEditing.AddLine(dialog, Vector2.zero, null);

            Assert.IsTrue(Validate().Any(w => w.LineId == null && w.Field == nameof(DialogAsset.StartNodeId)));
        }

        [Test]
        public void ValidatorWarnsAboutUnreachableLines()
        {
            DialogAsset.Line a = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogAsset.Line orphan = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogGraphEditing.SetStart(dialog, a.Id);

            IReadOnlyList<DialogWarning> warnings = Validate();

            Assert.IsTrue(warnings.Any(w => w.LineId == orphan.Id));
            Assert.IsFalse(warnings.Any(w => w.LineId == a.Id));
        }

        [Test]
        public void ValidatorWarnsAboutDeadEndAnswers()
        {
            DialogAsset.Line a = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogGraphEditing.SetStart(dialog, a.Id);
            DialogAsset.Answer secret = DialogGraphEditing.AddAnswer(dialog, a.Id);
            secret.VisibleIf = "Gold > 10";

            Assert.IsTrue(Validate().Any(w => w.LineId == a.Id && w.AnswerId == null));

            DialogAsset.Answer plain = DialogGraphEditing.AddAnswer(dialog, a.Id);
            Assert.IsFalse(Validate().Any(w => w.LineId == a.Id && w.AnswerId == null), plain.Id);
        }

        [Test]
        public void ValidatorReportsExpressionErrorsWithPosition()
        {
            DialogAsset.Line a = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogGraphEditing.SetStart(dialog, a.Id);
            DialogAsset.Answer answer = DialogGraphEditing.AddAnswer(dialog, a.Id);
            answer.EnabledIf = "Gold >= ";
            answer.Actions = "Gold = ";

            List<DialogWarning> errors = Validate().Where(w => w.Error != null).ToList();

            DialogWarning enabled = errors.Single(w => w.Field == nameof(DialogAsset.Answer.EnabledIf));
            Assert.AreEqual(answer.Id, enabled.AnswerId);
            Assert.AreEqual(8, enabled.Error.Position);
            Assert.IsTrue(errors.Any(w => w.Field == nameof(DialogAsset.Answer.Actions)));
        }

        [Test]
        public void ValidatorReportsMissingTargetsAndDuplicateGroups()
        {
            DialogAsset.Line a = DialogGraphEditing.AddLine(dialog, Vector2.zero, null);
            DialogGraphEditing.SetStart(dialog, a.Id);
            a.NextId = "gone";
            DialogGraphEditing.AddGroup(dialog, "Same", Vector2.zero);
            DialogGraphEditing.AddGroup(dialog, "Same", Vector2.zero);

            IReadOnlyList<DialogWarning> warnings = Validate();

            Assert.IsTrue(warnings.Any(w => w.LineId == a.Id && w.Field == nameof(DialogAsset.Line.NextId)));
            Assert.IsTrue(warnings.Any(w => w.Message.Contains("Same")));
        }

        [Test]
        public void UniqueNamesNeverOverwrite()
        {
            AssetDatabase.CreateFolder("Assets", "DialogSystemTestTemp");
            string first = UniqueAssetNames.NewAssetPath(TempFolder, "NewDialog");
            Assert.AreEqual(TempFolder + "/NewDialog.asset", first);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DialogAsset>(), first);

            string second = UniqueAssetNames.NewAssetPath(TempFolder, "NewDialog");

            Assert.AreEqual(TempFolder + "/NewDialog 1.asset", second);
        }

        [Test]
        public void DebouncerFiresOnceAfterQuietPeriod()
        {
            double now = 0;
            var debouncer = new Debouncer(0.5, () => now);

            Assert.IsFalse(debouncer.Due());
            debouncer.Poke();
            now = 0.3;
            debouncer.Poke();
            now = 0.7;
            Assert.IsFalse(debouncer.Due());
            now = 0.8;
            Assert.IsTrue(debouncer.Due());
            Assert.IsFalse(debouncer.Due());
            now = 5;
            Assert.IsFalse(debouncer.Due());
        }

        [Test]
        public void EditsAreUndoable()
        {
            Undo.IncrementCurrentGroup();
            DialogGraphEditing.AddLine(dialog, Vector2.zero, author);
            Assert.AreEqual(1, dialog.Lines.Count);

            Undo.PerformUndo();

            Assert.AreEqual(0, dialog.Lines.Count);
        }
    }
}
