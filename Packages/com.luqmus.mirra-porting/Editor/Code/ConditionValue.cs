namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// What a preprocessor condition evaluates to under player build assumptions. Unknown is a
    /// value of its own: most symbols are neither known to be defined nor known not to be.
    /// </summary>
    internal enum ConditionValue
    {
        False = 0,
        True = 1,
        Unknown = 2,
    }
}
