namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// How badly a finding blocks moderation. Explicit values: the order is part of the report
    /// contract (findings are sorted by severity) and is serialized by name into JSON.
    /// </summary>
    internal enum Severity
    {
        Error = 0,
        Warning = 1,
        Info = 2,
    }
}
