using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    public class ConditionEvaluatorTests
    {
        [Test]
        public void TryEvaluate_EditorSymbolsAreNotDefinedInAPlayerBuild()
        {
            AssertValue("UNITY_EDITOR", ConditionValue.False);
            AssertValue("UNITY_EDITOR_WIN", ConditionValue.False);
            AssertValue("UNITY_EDITOR_OSX", ConditionValue.False);
            AssertValue("UNITY_EDITOR_LINUX", ConditionValue.False);
            AssertValue("UNITY_EDITOR_64", ConditionValue.False);
        }

        [Test]
        public void TryEvaluate_LiteralsAndUnknownSymbols()
        {
            AssertValue("true", ConditionValue.True);
            AssertValue("false", ConditionValue.False);
            AssertValue("DEBUG", ConditionValue.Unknown);
            AssertValue("UNITY_WEBGL", ConditionValue.Unknown);
        }

        [Test]
        public void TryEvaluate_Operators()
        {
            AssertValue("!UNITY_EDITOR", ConditionValue.True);
            AssertValue("!(UNITY_EDITOR)", ConditionValue.True);
            AssertValue("!!UNITY_EDITOR", ConditionValue.False);
            AssertValue("UNITY_EDITOR && DEBUG", ConditionValue.False);
            AssertValue("DEBUG && UNITY_EDITOR", ConditionValue.False);
            AssertValue("UNITY_EDITOR || UNITY_WEBGL", ConditionValue.Unknown);
            AssertValue("UNITY_EDITOR == false", ConditionValue.True);
            AssertValue("UNITY_EDITOR != false", ConditionValue.False);
            AssertValue("DEBUG == UNITY_EDITOR", ConditionValue.Unknown);
        }

        [Test]
        public void TryEvaluate_Grouping()
        {
            AssertValue("(DEBUG && TRACE) || UNITY_EDITOR", ConditionValue.Unknown);
            AssertValue("(DEBUG || UNITY_EDITOR) && UNITY_EDITOR", ConditionValue.False);
            AssertValue("(((UNITY_EDITOR)))", ConditionValue.False);
        }

        [Test]
        public void TryEvaluate_TrailingCommentIsDropped()
        {
            AssertValue("UNITY_EDITOR // note", ConditionValue.False);
            AssertValue("UNITY_EDITOR /* note */", ConditionValue.False);
        }

        [TestCase("A &&")]
        [TestCase("(A")]
        [TestCase("A B")]
        [TestCase("A & B")]
        [TestCase("A)")]
        [TestCase("1")]
        [TestCase("\"A\"")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("// only a comment")]
        public void TryEvaluate_BrokenConditionStaysUnknown(string condition)
        {
            ConditionValue value;

            Assert.IsFalse(ConditionEvaluator.TryEvaluate(condition, out value), condition);
            Assert.AreEqual(ConditionValue.Unknown, value);
        }

        private static void AssertValue(string condition, ConditionValue expected)
        {
            ConditionValue value;

            Assert.IsTrue(ConditionEvaluator.TryEvaluate(condition, out value), "Did not parse: " + condition);
            Assert.AreEqual(expected, value, condition);
        }
    }
}
