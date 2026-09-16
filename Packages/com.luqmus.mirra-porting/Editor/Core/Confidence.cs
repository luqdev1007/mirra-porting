namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// How sure the check is that the finding is real. Explicit values: used as a sort key and
    /// serialized by name into JSON.
    /// </summary>
    internal enum Confidence
    {
        High = 0,
        Medium = 1,
        Low = 2,
    }
}
