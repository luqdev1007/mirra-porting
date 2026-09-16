using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Core
{
    /// <summary>
    /// Catalog of every rule the scanner can report. The window and the report read their text
    /// from here; checks reference the fields directly.
    /// </summary>
    internal static class Rules
    {
        /// <summary>
        /// A check threw. The scan keeps going, so this is a warning about incomplete coverage
        /// rather than an error in the project. The exception text goes into the finding message.
        /// </summary>
        internal static readonly Rule ScanInternalError = new Rule(
            "SCAN.INTERNAL_ERROR",
            Categories.Scan,
            Severity.Warning,
            "Проверка завершилась с ошибкой, часть проекта могла остаться непросканированной",
            "Сообщите об ошибке разработчику пакета вместе с текстом исключения из сообщения находки");

        private static readonly Rule[] AllRules =
        {
            ScanInternalError,
        };

        private static readonly Dictionary<string, Rule> ById = BuildIndex(AllRules);

        /// <summary>Every known rule, in declaration order.</summary>
        internal static IReadOnlyList<Rule> All
        {
            get { return AllRules; }
        }

        /// <summary>Rule by id, or null when the id is unknown.</summary>
        internal static Rule Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            Rule rule;
            return ById.TryGetValue(id, out rule) ? rule : null;
        }

        private static Dictionary<string, Rule> BuildIndex(Rule[] rules)
        {
            var index = new Dictionary<string, Rule>(rules.Length, StringComparer.Ordinal);
            foreach (Rule rule in rules)
            {
                index.Add(rule.Id, rule);
            }

            return index;
        }
    }
}
