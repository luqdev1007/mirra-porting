namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// A single scanner rule. Holds the user facing text so that the window and the report take
    /// the wording from one place instead of duplicating it.
    /// </summary>
    internal sealed class Rule
    {
        internal Rule(string id, string category, Severity severity, string message, string suggestion)
        {
            Id = id;
            Category = category;
            Severity = severity;
            Message = message;
            Suggestion = suggestion;
        }

        /// <summary>Stable identifier, for example "API.TIMESCALE_WRITE".</summary>
        internal string Id { get; }

        /// <summary>One of <see cref="Categories"/>.</summary>
        internal string Category { get; }

        /// <summary>Default severity. A finding may lower it, see <see cref="Finding.FromRule"/>.</summary>
        internal Severity Severity { get; }

        /// <summary>What is wrong, in Russian.</summary>
        internal string Message { get; }

        /// <summary>What to use instead, in Russian. Empty when there is nothing to suggest.</summary>
        internal string Suggestion { get; }
    }
}
