using System.Collections.Generic;
using MIDIFrogs.DialogSystem.Core;
using MIDIFrogs.DialogSystem.Core.Expressions;
using NUnit.Framework;

namespace MIDIFrogs.DialogSystem.Tests
{
    public sealed class ExpressionTests
    {
        private DialogFunctions functions;
        private DialogContext context;

        [SetUp]
        public void SetUp()
        {
            functions = new DialogFunctions();
            context = new DialogContext();
        }

        private bool Eval(string source)
        {
            DialogExpression expression = DialogExpression.Parse(source, functions, out ExpressionError error);
            Assert.IsNull(error, error?.Message);
            return expression.Evaluate(context);
        }

        [Test]
        public void PrecedenceMatchesSpecExample()
        {
            const string source = "Gold > 10 && !QuestCompleted || Role == \"Test\"";
            context.SetValue("Gold", 12);
            context.SetValue("QuestCompleted", false);
            context.SetValue("Role", "X");
            Assert.IsTrue(Eval(source));

            context.SetValue("QuestCompleted", true);
            Assert.IsFalse(Eval(source));

            context.SetValue("Role", "Test");
            Assert.IsTrue(Eval(source));
        }

        [Test]
        public void UnsetVariablesDefaultByComparison()
        {
            Assert.IsTrue(Eval("Gold == 0"));
            Assert.IsTrue(Eval("!Flag"));
            Assert.IsTrue(Eval("Name == \"\""));
        }

        [Test]
        public void TypeMismatchComparesFalse()
        {
            context.SetValue("Gold", 5);
            Assert.IsFalse(Eval("Gold > \"ten\""));
            Assert.IsFalse(Eval("Gold == \"5\""));
        }

        [Test]
        public void UnknownFunctionIsAnError()
        {
            Assert.IsNull(DialogExpression.Parse("HasItem(\"Sword\")", functions, out ExpressionError error));
            StringAssert.Contains("HasItem", error.Message);
            Assert.AreEqual(0, error.Position);
            Assert.AreEqual(7, error.Length);
        }

        [Test]
        public void WrongArgumentCountIsAnError()
        {
            functions.Register("HasItem", 1, args => DialogValue.FromBool(true));
            Assert.IsNull(DialogExpression.Parse("A && HasItem()", functions, out ExpressionError error));
            Assert.AreEqual(5, error.Position);
            StringAssert.Contains("HasItem", error.Message);
        }

        [Test]
        public void SyntaxErrorReportsPosition()
        {
            Assert.IsNull(DialogExpression.Parse("Gold > ", functions, out ExpressionError error));
            Assert.AreEqual(7, error.Position);

            Assert.IsNull(DialogExpression.Parse("(a && b", functions, out error));
            StringAssert.Contains(")", error.Message);
        }

        [Test]
        public void RegisteredFunctionIsCalled()
        {
            functions.Register("HasItem", 1, args => DialogValue.FromBool(args[0].String == "Sword"));
            Assert.IsTrue(Eval("HasItem(\"Sword\")"));
            Assert.IsFalse(Eval("HasItem(\"Shield\")"));
        }

        [Test]
        public void StringEscapes()
        {
            context.SetValue("Quote", "say \"hi\" \\ ok");
            Assert.IsTrue(Eval("Quote == \"say \\\"hi\\\" \\\\ ok\""));
        }

        [Test]
        public void ShortCircuitSkipsRightSide()
        {
            var calls = new List<string>();
            functions.Register("Touch", 0, args =>
            {
                calls.Add("touched");
                return DialogValue.FromBool(true);
            });
            Assert.IsFalse(Eval("false && Touch()"));
            Assert.IsTrue(Eval("true || Touch()"));
            Assert.IsEmpty(calls);
        }

        [Test]
        public void ScriptAssignsAndIncrements()
        {
            DialogScript script = DialogScript.Parse("Gold = 10; Gold -= 3; Name = \"A\"; Name += \"B\";", functions, out ExpressionError error);
            Assert.IsNull(error, error?.Message);
            script.Execute(context);
            Assert.IsTrue(Eval("Gold == 7"));
            Assert.IsTrue(Eval("Name == \"AB\""));
        }

        [Test]
        public void EmptySourcesAreTrueAndNoOp()
        {
            Assert.IsTrue(Eval("   "));
            DialogScript script = DialogScript.Parse("", functions, out ExpressionError error);
            Assert.IsNull(error);
            script.Execute(context);
            Assert.IsEmpty(context.Records);
        }
    }
}
