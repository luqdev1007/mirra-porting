using Luqmus.MirraPorting.Code;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Code
{
    /// <summary>
    /// The truth tables are spelled out inside the tests rather than as TestCase arguments:
    /// ConditionValue is internal, and a public test method cannot take it as a parameter.
    /// </summary>
    public class ConditionLogicTests
    {
        [Test]
        public void Not_TruthTable()
        {
            Assert.AreEqual(ConditionValue.False, ConditionLogic.Not(ConditionValue.True));
            Assert.AreEqual(ConditionValue.True, ConditionLogic.Not(ConditionValue.False));
            Assert.AreEqual(ConditionValue.Unknown, ConditionLogic.Not(ConditionValue.Unknown));
        }

        [Test]
        public void And_TruthTable()
        {
            AssertAnd(ConditionValue.True, ConditionValue.True, ConditionValue.True);
            AssertAnd(ConditionValue.True, ConditionValue.False, ConditionValue.False);
            AssertAnd(ConditionValue.False, ConditionValue.False, ConditionValue.False);
            AssertAnd(ConditionValue.False, ConditionValue.Unknown, ConditionValue.False);
            AssertAnd(ConditionValue.Unknown, ConditionValue.False, ConditionValue.False);
            AssertAnd(ConditionValue.True, ConditionValue.Unknown, ConditionValue.Unknown);
            AssertAnd(ConditionValue.Unknown, ConditionValue.Unknown, ConditionValue.Unknown);
        }

        [Test]
        public void Or_TruthTable()
        {
            AssertOr(ConditionValue.False, ConditionValue.False, ConditionValue.False);
            AssertOr(ConditionValue.True, ConditionValue.False, ConditionValue.True);
            AssertOr(ConditionValue.True, ConditionValue.Unknown, ConditionValue.True);
            AssertOr(ConditionValue.Unknown, ConditionValue.True, ConditionValue.True);
            AssertOr(ConditionValue.False, ConditionValue.Unknown, ConditionValue.Unknown);
            AssertOr(ConditionValue.Unknown, ConditionValue.Unknown, ConditionValue.Unknown);
        }

        [Test]
        public void AreEqual_TruthTable()
        {
            AssertEqual(ConditionValue.True, ConditionValue.True, ConditionValue.True);
            AssertEqual(ConditionValue.False, ConditionValue.False, ConditionValue.True);
            AssertEqual(ConditionValue.True, ConditionValue.False, ConditionValue.False);
            AssertEqual(ConditionValue.Unknown, ConditionValue.True, ConditionValue.Unknown);
            AssertEqual(ConditionValue.True, ConditionValue.Unknown, ConditionValue.Unknown);
            AssertEqual(ConditionValue.Unknown, ConditionValue.Unknown, ConditionValue.Unknown);
        }

        [Test]
        public void AreNotEqual_IsTheNegationOfAreEqual()
        {
            Assert.AreEqual(
                ConditionValue.False, ConditionLogic.AreNotEqual(ConditionValue.True, ConditionValue.True));
            Assert.AreEqual(
                ConditionValue.True, ConditionLogic.AreNotEqual(ConditionValue.True, ConditionValue.False));
            Assert.AreEqual(
                ConditionValue.Unknown, ConditionLogic.AreNotEqual(ConditionValue.Unknown, ConditionValue.False));
        }

        private static void AssertAnd(ConditionValue left, ConditionValue right, ConditionValue expected)
        {
            Assert.AreEqual(expected, ConditionLogic.And(left, right), left + " && " + right);
        }

        private static void AssertOr(ConditionValue left, ConditionValue right, ConditionValue expected)
        {
            Assert.AreEqual(expected, ConditionLogic.Or(left, right), left + " || " + right);
        }

        private static void AssertEqual(ConditionValue left, ConditionValue right, ConditionValue expected)
        {
            Assert.AreEqual(expected, ConditionLogic.AreEqual(left, right), left + " == " + right);
        }
    }
}
