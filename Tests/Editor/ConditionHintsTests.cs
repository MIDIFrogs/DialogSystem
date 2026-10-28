using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using MIDIFrogs.DialogSystem.Core.Hints;
using NUnit.Framework;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class ConditionHintsTests
    {
        private sealed class MapResolver : IDialogTextResolver
        {
            public readonly Dictionary<string, string> Map = new();

            public string Resolve(string key, string sourceText) => Map.TryGetValue(key, out string text) ? text : sourceText;
        }

        private DialogFunctions functions;
        private DialogContext context;
        private MapResolver text;

        [SetUp]
        public void SetUp()
        {
            functions = new DialogFunctions();
            context = new DialogContext();
            text = new MapResolver();
        }

        private IReadOnlyList<string> Describe(string source)
        {
            DialogExpression expression = DialogExpression.Parse(source, functions, out ExpressionError error);
            Assert.IsNull(error, error?.Message);
            return ConditionHints.Describe(expression, context, functions, text);
        }

        [Test]
        public void FailedClausesOnly()
        {
            context.SetValue("Gold", 3);
            context.SetValue("HasKey", true);
            CollectionAssert.AreEqual(new[] { "Gold ≥ 10" }, Describe("Gold >= 10 && HasKey"));
        }

        [Test]
        public void VariableNamesAreLocalized()
        {
            text.Map["dialog.var.Gold"] = "Золото";
            CollectionAssert.AreEqual(new[] { "Золото ≥ 10" }, Describe("Gold >= 10"));
        }

        [Test]
        public void FunctionHintIsUsed()
        {
            functions.Register("HasItem", 1, args => DialogValue.FromBool(false), "Нужен меч");
            CollectionAssert.AreEqual(new[] { "Нужен меч" }, Describe("HasItem(\"Sword\")"));
        }

        [Test]
        public void OrClauseListsAlternatives()
        {
            CollectionAssert.AreEqual(new[] { "одно из: A / B" }, Describe("A || B"));
        }

        [Test]
        public void SatisfiedExpressionHasNoLines()
        {
            context.SetValue("Gold", 20);
            CollectionAssert.IsEmpty(Describe("Gold >= 10"));
        }
    }
}
