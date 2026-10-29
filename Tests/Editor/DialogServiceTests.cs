using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Unity.Integration;
using MIDIFrogs.DialogSystem.Unity.View;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class DialogServiceTests
    {
        /// <summary>Answers immediately: picks the answer named in <see cref="Pick"/>, otherwise "continue".</summary>
        private sealed class FakeView : IDialogView
        {
            public readonly List<string> Shown = new();
            public string Pick;
            public bool WaitForever;
            public CancellationToken LastToken;
            public int Hides;
            public int Cleanups;
            public bool ThrowOnShow;
            public readonly List<int> CleanupsWhenShown = new();

            public void Show()
            {
                if (ThrowOnShow)
                {
                    throw new InvalidOperationException("broken view");
                }
            }

            public void Hide() => Hides++;

            public async UniTask<DialogChoice> ShowLine(DialogNode node, IReadOnlyList<ChoiceState> choices,
                IDialogTextResolver text, CancellationToken token)
            {
                Shown.Add(node.Text.Content);
                CleanupsWhenShown.Add(Cleanups);
                LastToken = token;
                if (WaitForever)
                {
                    // Like a real view: notices cancellation on its next frame, then cleans up.
                    try
                    {
                        while (true)
                        {
                            await UniTask.Yield(PlayerLoopTiming.Update, token);
                        }
                    }
                    finally
                    {
                        Cleanups++;
                    }
                }
                return choices.FirstOrDefault(c => c.Choice.Text == Pick)?.Choice;
            }
        }

        private FakeView view;
        private DialogService service;

        [SetUp]
        public void SetUp()
        {
            view = new FakeView();
            service = new DialogService(view);
        }

        private static DialogNode Line(string text) => new() { Id = text, Text = new StyledText { Content = text } };

        private DialogChoice Answer(string text, DialogNode target, string actions = "") => new()
        {
            Id = text,
            Text = text,
            Actions = DialogScript.Parse(actions, service.Functions, out _),
            Target = target,
        };

        [Test]
        [Timeout(2000)]
        public void LinearDialogPlaysToTheEnd()
        {
            DialogNode a = Line("a"), b = Line("b");
            a.Next = b;

            UniTask play = service.Play(new Dialog { StartNode = a }, new DialogContext());

            Assert.AreEqual(UniTaskStatus.Succeeded, play.Status);
            CollectionAssert.AreEqual(new[] { "a", "b" }, view.Shown);
            Assert.AreEqual(1, view.Hides);
        }

        [Test]
        [Timeout(2000)]
        public void ChoiceIsApplied()
        {
            DialogNode a = Line("a"), yes = Line("yes"), no = Line("no");
            a.Choices.Add(Answer("Y", yes));
            a.Choices.Add(Answer("N", no));
            view.Pick = "N";

            service.Play(new Dialog { StartNode = a }, new DialogContext());

            CollectionAssert.AreEqual(new[] { "a", "no" }, view.Shown);
        }

        [UnityTest]
        public IEnumerator StopEndsPlayWithoutThrowing() => UniTask.ToCoroutine(async () =>
        {
            view.WaitForever = true;
            UniTask play = service.Play(new Dialog { StartNode = Line("a") }, new DialogContext());
            await UniTask.Yield();
            service.Stop();
            await play;
            Assert.IsFalse(service.IsPlaying);
            Assert.AreEqual(1, view.Hides);
        });

        [Test]
        [Timeout(2000)]
        public void SkipStrategyNeverLoops()
        {
            DialogNode a = Line("a");
            a.Choices.Add(new DialogChoice { Id = "locked", Text = "locked", EnabledIf = DialogExpression.Constant(false), Target = Line("b") });
            service.PlaybackStrategy = new SkipStrategy();

            UniTask play = service.Play(new Dialog { StartNode = a }, new DialogContext());

            Assert.AreEqual(UniTaskStatus.Succeeded, play.Status);
        }

        [UnityTest]
        public IEnumerator AutoplayCancelsTheViewWhenTimerWins() => UniTask.ToCoroutine(async () =>
        {
            view.WaitForever = true;
            service.PlaybackStrategy = new AutoplayStrategy(0.05f);
            await service.Play(new Dialog { StartNode = Line("a") }, new DialogContext())
                .Timeout(TimeSpan.FromSeconds(2), DelayType.Realtime);
            Assert.IsTrue(view.LastToken.IsCancellationRequested);
        });

        [UnityTest]
        public IEnumerator AutoplayLetsTheViewCleanUpBeforeTheNextLine() => UniTask.ToCoroutine(async () =>
        {
            view.WaitForever = true;
            service.PlaybackStrategy = new AutoplayStrategy(0.05f);
            DialogNode a = Line("a"), b = Line("b");
            a.Next = b;
            await service.Play(new Dialog { StartNode = a }, new DialogContext())
                .Timeout(TimeSpan.FromSeconds(3), DelayType.Realtime);
            CollectionAssert.AreEqual(new[] { 0, 1 }, view.CleanupsWhenShown);
        });

        [Test]
        public void BrokenViewDoesNotLeaveTheServicePlaying()
        {
            view.ThrowOnShow = true;
            Assert.Throws<InvalidOperationException>(() =>
                service.Play(new Dialog { StartNode = Line("a") }, new DialogContext()).GetAwaiter().GetResult());
            Assert.IsFalse(service.IsPlaying);
        }

        [Test]
        [Timeout(2000)]
        public void ContextIsTheCallers()
        {
            DialogNode a = Line("a");
            a.Choices.Add(Answer("buy", null, "Bought = true"));
            view.Pick = "buy";
            var context = new DialogContext();

            service.Play(new Dialog { StartNode = a }, context);

            Assert.IsTrue(context.TryGetValue("Bought", out object bought) && (bool)bought);
        }
    }
}
