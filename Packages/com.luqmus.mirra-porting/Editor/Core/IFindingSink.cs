namespace Luqmus.MirraPorting.Core
{
    /// <summary>Where a check puts what it found.</summary>
    internal interface IFindingSink
    {
        void Add(Finding finding);
    }
}
