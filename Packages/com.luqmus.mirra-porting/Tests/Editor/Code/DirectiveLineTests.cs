using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class DirectiveLineTests
    {
        [Test]
        public void Parse_ConditionalKeywords()
        {
            AssertKind("#if X", DirectiveKind.If);
            AssertKind("#  if X", DirectiveKind.If);
            AssertKind("#elif X", DirectiveKind.Elif);
            AssertKind("#else", DirectiveKind.Else);
            AssertKind("#endif", DirectiveKind.EndIf);
        }

        [Test]
        public void Parse_EverythingElseIsOther()
        {
            AssertKind("#region Fields", DirectiveKind.Other);
            AssertKind("#endregion", DirectiveKind.Other);
            AssertKind("#pragma warning disable 0649", DirectiveKind.Other);
            AssertKind("#define A", DirectiveKind.Other);
            AssertKind("#undef A", DirectiveKind.Other);
            AssertKind("#", DirectiveKind.Other);
        }

        [Test]
        public void Parse_ArgumentIsEverythingAfterTheKeyword()
        {
            Assert.AreEqual("UNITY_EDITOR && DEBUG", Parse("#if UNITY_EDITOR && DEBUG").Argument);
            Assert.AreEqual("X", Parse("#  if X").Argument);
            Assert.AreEqual(string.Empty, Parse("#endif").Argument);
        }

        [Test]
        public void Parse_TrailingCommentStaysInTheArgumentAndIsDroppedWhenItIsLexed()
        {
            DirectiveLine directive = Parse("#if UNITY_EDITOR // note");

            Assert.AreEqual("UNITY_EDITOR // note", directive.Argument);

            ConditionValue value;
            Assert.IsTrue(ConditionEvaluator.TryEvaluate(directive.Argument, out value));
            Assert.AreEqual(ConditionValue.False, value);
        }

        private static void AssertKind(string line, DirectiveKind expected)
        {
            Assert.AreEqual(expected, Parse(line).Kind, line);
        }

        private static DirectiveLine Parse(string line)
        {
            LexResult result = CSharpLexer.Lex(line);

            Assert.AreEqual(1, result.Tokens.Count, "Expected one directive token for: " + line);
            Assert.AreEqual(TokenKind.Directive, result.Tokens[0].Kind, line);
            return DirectiveLine.Parse(result.Tokens[0]);
        }
    }
}
