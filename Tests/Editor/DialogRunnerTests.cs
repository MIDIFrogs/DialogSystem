using System.Collections.Generic;
using System.Linq;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using NUnit.Framework;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class DialogRunnerTests
    {
        private DialogFunctions functions;
        private DialogContext context;
        private DialogRunner runner;

        [SetUp]
        public void SetUp()
        {
            functions = new DialogFunctions();
            context = new DialogContext();
            runner = new DialogRunner(context, functions, SourceTextResolver.Instance);
        }

        private DialogNode Line(string id) => new() { Id = id, Text = new StyledText { Content = id } };

        private DialogChoice Answer(string text, DialogNode target, string visibleIf = "", string enabledIf = "",
            string actions = "", string hint = null)
        {
            return new DialogChoice
            {
                Id = text,
                Text = text,
                VisibleIf = Parse(visibleIf),
                EnabledIf = Parse(enabledIf),
                Actions = DialogScript.Parse(actions, functions, out _),
                Hint = hint,
                Target = target,
            };
        }

        private DialogExpression Parse(string source)
        {
            DialogExpression expression = DialogExpression.Parse(source, functions, out ExpressionError error);
            Assert.IsNull(error, error?.Message);
            return expression;
        }

        private static Dialog Start(DialogNode node) => new() { StartNode = node };

        [Test]
        public void LinearLinesFollowNext()
        {
            DialogNode a = Line("a"), b = Line("b");
            a.Next = b;
            runner.Start(Start(a));
            Assert.IsFalse(runner.IsWaitingForChoice);
            runner.Advance();
            Assert.AreSame(b, runner.CurrentNode);
        }

        [Test]
        public void LineWithoutAnswersOrNextEnds()
        {
            runner.Start(Start(Line("a")));
            runner.Advance();
            Assert.IsTrue(runner.IsFinished);
        }

        [Test]
        public void HiddenAnswersAreAbsent()
        {
            DialogNode a = Line("a");
            a.Choices.Add(Answer("open", null));
            a.Choices.Add(Answer("secret", null, visibleIf: "Knows"));
            runner.Start(Start(a));
            CollectionAssert.AreEqual(new[] { "open" }, runner.Choices.Select(c => c.Choice.Text));
        }

        [Test]
        public void DisabledAnswersCarryHints()
        {
            DialogNode a = Line("a");
            a.Choices.Add(Answer("pay", null, enabledIf: "Gold >= 10"));
            a.Choices.Add(Answer("custom", null, enabledIf: "Gold >= 10", hint: "Нужны деньги"));
            context.SetValue("Gold", 3);
            runner.Start(Start(a));
            Assert.IsFalse(runner.Choices[0].Enabled);
            CollectionAssert.AreEqual(new[] { "Gold ≥ 10" }, runner.Choices[0].Hint);
            CollectionAssert.AreEqual(new[] { "Нужны деньги" }, runner.Choices[1].Hint);
        }

        [Test]
        public void AllAnswersHiddenFallsBackToNext()
        {
            DialogNode a = Line("a"), b = Line("b");
            a.Next = b;
            a.Choices.Add(Answer("secret", null, visibleIf: "false"));
            runner.Start(Start(a));
            Assert.IsEmpty(runner.Choices);
            Assert.IsFalse(runner.IsWaitingForChoice);
            runner.Advance();
            Assert.AreSame(b, runner.CurrentNode);
        }

        [Test]
        public void LineWithOnlyDisabledAnswersAndNoNextEnds()
        {
            DialogNode a = Line("a");
            a.Choices.Add(Answer("pay", Line("b"), enabledIf: "false"));
            runner.Start(Start(a));
            Assert.IsFalse(runner.IsWaitingForChoice);
            runner.Advance();
            Assert.IsTrue(runner.IsFinished);
        }

        [Test]
        public void ChooseRejectsUnavailableChoice()
        {
            DialogNode a = Line("a"), b = Line("b");
            DialogChoice locked = Answer("pay", b, enabledIf: "false");
            DialogChoice hidden = Answer("secret", b, visibleIf: "false");
            a.Choices.Add(locked);
            a.Choices.Add(hidden);
            runner.Start(Start(a));
            Assert.IsFalse(runner.Choose(locked));
            Assert.IsFalse(runner.Choose(hidden));
            Assert.IsFalse(runner.Choose(Answer("foreign", b)));
            Assert.AreSame(a, runner.CurrentNode);
        }

        [Test]
        public void ChooseRunsActionsThenMoves()
        {
            DialogNode a = Line("a"), b = Line("b");
            DialogChoice buy = Answer("buy", b, actions: "Gold -= 10; Bought = true");
            a.Choices.Add(buy);
            context.SetValue("Gold", 15);
            runner.Start(Start(a));
            Assert.IsTrue(runner.Choose(buy));
            Assert.AreSame(b, runner.CurrentNode);
            Assert.IsTrue(context.TryGetValue("Gold", out object gold) && (double)gold == 5d);
            Assert.IsTrue(context.TryGetValue("Bought", out object bought) && (bool)bought);
        }

        [Test]
        public void ChoicesAreEvaluatedOnce()
        {
            int calls = 0;
            functions.Register("Count", 0, args =>
            {
                calls++;
                return DialogValue.FromBool(true);
            });
            DialogNode a = Line("a");
            a.Choices.Add(Answer("x", null, visibleIf: "Count()"));
            runner.Start(Start(a));
            _ = runner.Choices;
            _ = runner.Choices;
            _ = runner.IsWaitingForChoice;
            Assert.AreEqual(1, calls);
        }
    }
}
