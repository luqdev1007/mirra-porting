namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// What kind of member a table row describes. A method mentioned without parentheses is a
    /// method group, which is the same problem as calling it, so methods report reads as calls.
    /// </summary>
    internal enum ApiMemberKind
    {
        Property = 0,
        Method = 1,
    }
}
