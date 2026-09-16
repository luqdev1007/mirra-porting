namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Three valued logic over <see cref="ConditionValue"/>. Unknown means "could go either way",
    /// so it only survives where the other operand cannot decide the answer on its own.
    /// </summary>
    internal static class ConditionLogic
    {
        internal static ConditionValue Not(ConditionValue value)
        {
            if (value == ConditionValue.True)
            {
                return ConditionValue.False;
            }

            if (value == ConditionValue.False)
            {
                return ConditionValue.True;
            }

            return ConditionValue.Unknown;
        }

        internal static ConditionValue And(ConditionValue left, ConditionValue right)
        {
            if (left == ConditionValue.False || right == ConditionValue.False)
            {
                return ConditionValue.False;
            }

            if (left == ConditionValue.True && right == ConditionValue.True)
            {
                return ConditionValue.True;
            }

            return ConditionValue.Unknown;
        }

        internal static ConditionValue Or(ConditionValue left, ConditionValue right)
        {
            if (left == ConditionValue.True || right == ConditionValue.True)
            {
                return ConditionValue.True;
            }

            if (left == ConditionValue.False && right == ConditionValue.False)
            {
                return ConditionValue.False;
            }

            return ConditionValue.Unknown;
        }

        internal static ConditionValue AreEqual(ConditionValue left, ConditionValue right)
        {
            if (left == ConditionValue.Unknown || right == ConditionValue.Unknown)
            {
                return ConditionValue.Unknown;
            }

            return left == right ? ConditionValue.True : ConditionValue.False;
        }

        internal static ConditionValue AreNotEqual(ConditionValue left, ConditionValue right)
        {
            return Not(AreEqual(left, right));
        }
    }
}
