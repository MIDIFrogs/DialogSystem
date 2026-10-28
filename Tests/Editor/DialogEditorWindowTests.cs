using System.Collections;
using MIDIFrogs.DialogSystem.Editor.Window;
using MIDIFrogs.DialogSystem.Unity.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class DialogEditorWindowTests
    {
        private const string TempFolder = "Assets/DialogWindowTestTemp";

        private DialogEditorWindow window;
        private DialogAsset dialog;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AssetDatabase.CreateFolder("Assets", "DialogWindowTestTemp");
            dialog = ScriptableObject.CreateInstance<DialogAsset>();
            dialog.Id = "window_test";
            AssetDatabase.CreateAsset(dialog, TempFolder + "/WindowTest.asset");
            window = EditorWindow.GetWindow<DialogEditorWindow>();
            yield return null;
            window.OpenDialog(dialog);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            window.Close();
            yield return null;
            AssetDatabase.DeleteAsset(TempFolder);
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DropOnEmptySpaceCreatesLinkedLine()
        {
            window.Graph.CreateLinkedLine(PortRef.ForStart(), new Vector2(300, 200));
            yield return Frames(3);

            Assert.AreEqual(1, dialog.Lines.Count);
            Assert.AreEqual(dialog.Lines[0].Id, dialog.StartNodeId);
            Assert.AreSame(window.SelectedAuthor, dialog.Lines[0].Author);
        }

        [UnityTest]
        public IEnumerator TypingIsOneUndoStepPerPause()
        {
            window.Graph.CreateLinkedLine(PortRef.ForStart(), new Vector2(300, 200));
            yield return Frames(3);
            string lineId = dialog.Lines[0].Id;

            foreach (string typed in new[] { "П", "Пр", "При", "Прив" })
            {
                Undo.IncrementCurrentGroup();
                window.Change("Текст реплики", d => d.FindLine(lineId).Text = typed, ChangeKind.Typing, lineId + ".text");
                yield return null;
            }
            Assert.AreEqual("Прив", dialog.FindLine(lineId).Text);

            Undo.PerformUndo();
            yield return Frames(2);

            Assert.AreEqual(string.Empty, dialog.FindLine(lineId).Text);
            Assert.AreEqual(1, dialog.Lines.Count, "undo of the typing must not undo the line itself");
        }

        [UnityTest]
        public IEnumerator ChangesAreSavedAfterAPause()
        {
            window.Graph.CreateLinkedLine(PortRef.ForStart(), new Vector2(300, 200));
            yield return Frames(3);
            Assert.IsTrue(window.HasPendingSave);

            double end = EditorApplication.timeSinceStartup + 2;
            while (window.HasPendingSave && EditorApplication.timeSinceStartup < end)
            {
                yield return null;
            }

            Assert.IsFalse(window.HasPendingSave);
            Assert.IsFalse(EditorUtility.IsDirty(dialog));
        }
    }
}
