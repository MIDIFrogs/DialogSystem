using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Editor.Model;
using MIDIFrogs.DialogSystem.Editor.Settings;
using MIDIFrogs.DialogSystem.Unity.Data;
using NUnit.Framework;
using UnityEngine;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class DialogStringExportTests
    {
        private readonly List<Object> created = new();
        private DialogAsset dialog;
        private DialogAuthorAsset author;

        [SetUp]
        public void SetUp()
        {
            dialog = Create<DialogAsset>();
            dialog.Id = "meeting";
            author = Create<DialogAuthorAsset>();
            author.Id = "guard";
            author.DisplayName = "Стражник";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created)
            {
                Object.DestroyImmediate(item);
            }
            created.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var item = ScriptableObject.CreateInstance<T>();
            created.Add(item);
            return item;
        }

        private static Dictionary<string, string> ToMap(IReadOnlyList<DialogString> strings) =>
            strings.ToDictionary(s => s.Key, s => s.SourceText);

        [Test]
        public void CollectsLinesAnswersHintsAndAuthors()
        {
            var line = new DialogAsset.Line { Id = "a", Text = "Стой!", Author = author };
            line.Answers.Add(new DialogAsset.Answer { Id = "pay", Text = "Заплатить", Hint = "Нужно золото" });
            dialog.Lines.Add(line);

            Dictionary<string, string> map = ToMap(DialogStringExport.Collect(dialog, new[] { author }));

            Assert.AreEqual("Стой!", map["dialog.meeting.a"]);
            Assert.AreEqual("Заплатить", map["dialog.meeting.a.pay"]);
            Assert.AreEqual("Нужно золото", map["dialog.meeting.a.pay.hint"]);
            Assert.AreEqual("Стражник", map["dialog.author.guard"]);
            Assert.AreEqual("Продолжить…", map["dialog.continue"]);
            Assert.AreEqual("Требуется:", map["dialog.required"]);
        }

        [Test]
        public void SkipsEmptyTexts()
        {
            var line = new DialogAsset.Line { Id = "a", Text = "" };
            line.Answers.Add(new DialogAsset.Answer { Id = "x", Text = "Да", Hint = "" });
            dialog.Lines.Add(line);
            var nameless = Create<DialogAuthorAsset>();
            nameless.Id = "ghost";

            Dictionary<string, string> map = ToMap(DialogStringExport.Collect(dialog, new[] { nameless }));

            Assert.IsFalse(map.ContainsKey("dialog.meeting.a"));
            Assert.IsFalse(map.ContainsKey("dialog.meeting.a.x.hint"));
            Assert.IsFalse(map.ContainsKey("dialog.author.ghost"));
            Assert.AreEqual("Да", map["dialog.meeting.a.x"]);
        }

        [Test]
        public void KeysUseDialogId()
        {
            dialog.name = "Renamed file";
            dialog.Lines.Add(new DialogAsset.Line { Id = "a", Text = "Привет" });

            IReadOnlyList<DialogString> strings = DialogStringExport.Collect(dialog, null);

            Assert.IsTrue(strings.Any(s => s.Key == "dialog.meeting.a"));
            Assert.IsFalse(strings.Any(s => s.Key.Contains("Renamed")));
        }

        [Test]
        public void SettingsDefaultToRussianSource()
        {
            var settings = Create<DialogEditorSettings>();
            Assert.AreEqual("ru", settings.SourceLanguage);
            Assert.IsNull(settings.Sink);
        }
    }
}
