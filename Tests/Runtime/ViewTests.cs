using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.UI;
using MIDIFrogs.DialogSystem.Unity.View;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class ViewTests
    {
        private sealed class FakeInput : MonoBehaviour, IDialogInput
        {
            public bool Press;

            public bool AdvancePressedThisFrame()
            {
                bool pressed = Press;
                Press = false;
                return pressed;
            }
        }

        private readonly List<Object> created = new();
        private Canvas canvas;

        [SetUp]
        public void SetUp()
        {
            canvas = Track(new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster))).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Track(new GameObject("EventSystem", typeof(EventSystem)));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created)
            {
                if (item != null)
                {
                    Object.Destroy(item);
                }
            }
            created.Clear();
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }
            }
            Assert.Fail($"{target.GetType().Name} has no field {field}");
        }

        private GameObject Child(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private TMP_Text Text(string name, Transform parent) => Child(name, parent, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();

        private ResponseButton ButtonTemplate()
        {
            GameObject go = Child("ResponseTemplate", canvas.transform, typeof(RectTransform), typeof(Image), typeof(Button));
            var button = go.AddComponent<ResponseButton>();
            TMP_Text label = Text("Label", go.transform);
            GameObject hint = Child("Hint", go.transform, typeof(RectTransform));
            TMP_Text hintText = hint.AddComponent<TextMeshProUGUI>();
            Set(button, "text", label);
            Set(button, "button", go.GetComponent<Button>());
            Set(button, "hintRoot", hint);
            Set(button, "hintText", hintText);
            go.SetActive(false);
            return button;
        }

        private sealed class Rig
        {
            public DialogRootView Root;
            public TypewriterLineView Line;
            public ChoicesView Choices;
            public AuthorView Author;
            public ContinuePrompt Prompt;
            public FakeInput Input;
        }

        private Rig Build(bool optional)
        {
            GameObject rootGo = Child("Dialog", canvas.transform, typeof(RectTransform));
            rootGo.SetActive(false);
            var rig = new Rig { Root = rootGo.AddComponent<DialogRootView>() };

            GameObject lineGo = Child("Line", rootGo.transform, typeof(RectTransform));
            rig.Line = lineGo.AddComponent<TypewriterLineView>();
            Set(rig.Line, "text", lineGo.AddComponent<TextMeshProUGUI>());
            Set(rig.Line, "charactersPerSecond", 1000f);

            GameObject authorGo = Child("Author", rootGo.transform, typeof(RectTransform));
            rig.Author = authorGo.AddComponent<AuthorView>();
            Set(rig.Author, "nameText", Text("Name", authorGo.transform));
            Set(rig.Author, "avatarImage", Child("Avatar", authorGo.transform, typeof(RectTransform), typeof(Image)).GetComponent<Image>());

            GameObject choicesGo = Child("Choices", rootGo.transform, typeof(RectTransform));
            rig.Choices = choicesGo.AddComponent<ChoicesView>();
            Set(rig.Choices, "container", choicesGo.transform);
            Set(rig.Choices, "prefab", ButtonTemplate());

            Set(rig.Root, "lineViewObject", lineGo);
            Set(rig.Root, "authorViewObject", authorGo);
            Set(rig.Root, "choicesViewObject", choicesGo);

            if (optional)
            {
                GameObject promptGo = Child("Continue", rootGo.transform, typeof(RectTransform), typeof(Image));
                rig.Prompt = promptGo.AddComponent<ContinuePrompt>();
                Set(rig.Prompt, "label", Text("Label", promptGo.transform));
                rig.Input = rootGo.AddComponent<FakeInput>();
                Set(rig.Root, "continuePrompt", rig.Prompt);
                Set(rig.Root, "inputObject", rootGo);
            }

            rootGo.SetActive(true);
            return rig;
        }

        private static DialogNode Line(string text) => new() { Id = "l", Text = new StyledText { Content = text, Key = "k" } };

        private static ChoiceState State(string text, bool enabled, params string[] hint) =>
            new(new DialogChoice { Id = text, Text = text, TextKey = text }, enabled, hint);

        private static async UniTask WaitUntil(Func<bool> condition)
        {
            float end = Time.realtimeSinceStartup + 3f;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, end, "timed out");
                await UniTask.Yield();
            }
        }

        [UnityTest]
        public IEnumerator RootViewWorksWithoutOptionalViews() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: false);
            var states = new[] { State("Yes", true) };
            UniTask<DialogChoice> shown = rig.Root.ShowLine(Line("Hi"), states, SourceTextResolver.Instance, CancellationToken.None);
            await UniTask.Yield();
            rig.Choices.GetComponentInChildren<Button>().onClick.Invoke();
            DialogChoice picked = await shown;
            Assert.AreSame(states[0].Choice, picked);
        });

        [UnityTest]
        public IEnumerator ContinuePromptAppearsForLinearLines() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: true);
            UniTask<DialogChoice> shown = rig.Root.ShowLine(Line("Hi"), Array.Empty<ChoiceState>(), SourceTextResolver.Instance, CancellationToken.None);
            await WaitUntil(() => !rig.Line.IsTyping);
            Assert.IsTrue(rig.Prompt.gameObject.activeSelf);
            StringAssert.Contains("Продолжить", rig.Prompt.GetComponentInChildren<TMP_Text>().text);

            rig.Input.Press = true;
            Assert.IsNull(await shown);
            Assert.IsFalse(rig.Prompt.gameObject.activeSelf);
        });

        [UnityTest]
        public IEnumerator FirstPressCompletesTypingSecondContinues() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: true);
            Set(rig.Line, "charactersPerSecond", 1f);
            UniTask<DialogChoice> shown = rig.Root.ShowLine(Line("A long line"), Array.Empty<ChoiceState>(), SourceTextResolver.Instance, CancellationToken.None);
            await UniTask.Yield();
            Assert.IsTrue(rig.Line.IsTyping);

            rig.Input.Press = true;
            await WaitUntil(() => !rig.Line.IsTyping);
            await UniTask.Yield();
            Assert.AreEqual(UniTaskStatus.Pending, shown.Status);

            rig.Input.Press = true;
            Assert.IsNull(await shown);
        });

        [UnityTest]
        public IEnumerator DisabledChoiceShowsHintOnSelect() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: true);
            var states = new[] { State("Pay", false, "Gold ≥ 10"), State("Leave", true) };
            UniTask<DialogChoice> shown = rig.Root.ShowLine(Line("Hi"), states, SourceTextResolver.Instance, CancellationToken.None);
            await UniTask.Yield();
            ResponseButton locked = rig.Choices.GetComponentsInChildren<ResponseButton>()[0];
            GameObject hint = locked.transform.Find("Hint").gameObject;
            Assert.IsFalse(hint.activeSelf);

            EventSystem.current.SetSelectedGameObject(locked.gameObject);
            Assert.IsTrue(hint.activeSelf);
            StringAssert.Contains("Gold ≥ 10", hint.GetComponent<TMP_Text>().text);

            locked.GetComponent<Button>().onClick.Invoke();
            await UniTask.Yield();
            Assert.AreEqual(UniTaskStatus.Pending, shown.Status);
            rig.Choices.GetComponentsInChildren<Button>()[1].onClick.Invoke();
            Assert.AreSame(states[1].Choice, await shown);
        });

        [UnityTest]
        public IEnumerator FirstEnabledChoiceIsSelected() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: true);
            var states = new[] { State("Pay", false, "x"), State("Leave", true) };
            rig.Root.ShowLine(Line("Hi"), states, SourceTextResolver.Instance, CancellationToken.None).Forget();
            await UniTask.Yield();
            Assert.AreSame(rig.Choices.GetComponentsInChildren<Button>()[1].gameObject, EventSystem.current.currentSelectedGameObject);
        });

        [UnityTest]
        public IEnumerator TypewriterRevealsRichTextWithoutTags() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: false);
            var text = new StyledText { Content = "<b>Привет</b>, мир" };
            UniTask typing = rig.Line.ShowText(text, text.Content, null, null, CancellationToken.None);
            rig.Line.Complete();
            await typing;
            TMP_Text tmp = rig.Line.GetComponent<TMP_Text>();
            tmp.ForceMeshUpdate();
            Assert.AreEqual(11, tmp.textInfo.characterCount);
            Assert.GreaterOrEqual(tmp.maxVisibleCharacters, 11);
            Assert.IsFalse(tmp.GetParsedText().Contains("<"));
        });

        [UnityTest]
        public IEnumerator TextColorIsKeptUnlessOverridden() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: false);
            TMP_Text tmp = rig.Line.GetComponent<TMP_Text>();
            tmp.color = Color.red;
            await rig.Line.ShowText(new StyledText { Content = "a" }, "a", 0.001f, null, CancellationToken.None);
            Assert.AreEqual(Color.red, tmp.color);
            await rig.Line.ShowText(new StyledText { Content = "b", HasColor = true, Color = Color.blue }, "b", 0.001f, null, CancellationToken.None);
            Assert.AreEqual(Color.blue, tmp.color);
        });

        [UnityTest]
        public IEnumerator SkippedCrossFadeLeavesNoOverlay() => UniTask.ToCoroutine(async () =>
        {
            GameObject go = Child("Storyboard", canvas.transform, typeof(RectTransform), typeof(Image));
            go.SetActive(false);
            var view = go.AddComponent<AnimatedStoryboardView>();
            Set(view, "image", go.GetComponent<Image>());
            go.SetActive(true);
            var first = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var second = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            created.Add(first);
            created.Add(second);
            await view.ShowStoryboard(new TestBackground(first));
            view.ShowStoryboard(new TestBackground(second)).Forget();
            await UniTask.Yield();
            view.SkipAnimation();
            await UniTask.Yield();
            await UniTask.Yield();
            Assert.IsNull(go.transform.Find("Overlay"));
            Assert.AreSame(second, go.GetComponent<Image>().sprite);
            Assert.AreEqual(1f, go.GetComponent<Image>().color.a);
        });

        [UnityTest]
        public IEnumerator UnknownAuthorHidesAuthorView() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: false);
            Assert.DoesNotThrow(() => rig.Author.ShowAuthor(new DialogAuthor { Id = "ghost", Name = "Ghost" }, SourceTextResolver.Instance));
            Assert.IsFalse(rig.Author.transform.Find("Avatar").gameObject.activeSelf);
            rig.Author.ShowAuthor(null, SourceTextResolver.Instance);
            Assert.AreEqual(string.Empty, rig.Author.transform.Find("Name").GetComponent<TMP_Text>().text);
            await UniTask.Yield();
        });

        [UnityTest]
        public IEnumerator HideAfterViewDestroyedDoesNotThrow() => UniTask.ToCoroutine(async () =>
        {
            Rig rig = Build(optional: true);
            Object.Destroy(rig.Root.gameObject);
            await UniTask.Yield();
            Assert.DoesNotThrow(() => rig.Root.Hide());
        });

        private sealed class TestBackground : IBackgroundImage
        {
            private readonly Sprite sprite;

            public TestBackground(Sprite sprite) => this.sprite = sprite;

            public bool TryGetImage<T>(out T image) where T : class
            {
                image = sprite as T;
                return image != null;
            }
        }
    }
}
