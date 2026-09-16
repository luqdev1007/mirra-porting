using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// The parts of a file the player build never compiles, as sorted, non overlapping spans, plus
    /// what the directive pass could not make sense of.
    /// </summary>
    internal sealed class ConditionalRegions
    {
        internal static readonly ConditionalRegions Empty =
            new ConditionalRegions(new TextSpan[0], new LexDiagnostic[0]);

        private readonly IReadOnlyList<TextSpan> _spans;

        internal ConditionalRegions(IReadOnlyList<TextSpan> editorOnlySpans, IReadOnlyList<LexDiagnostic> diagnostics)
        {
            _spans = editorOnlySpans;
            Diagnostics = diagnostics;
        }

        internal IReadOnlyList<TextSpan> EditorOnlySpans
        {
            get { return _spans; }
        }

        internal IReadOnlyList<LexDiagnostic> Diagnostics { get; }

        /// <summary>True when the offset falls in a branch the player build drops.</summary>
        internal bool IsEditorOnlyAt(int start)
        {
            int low = 0;
            int high = _spans.Count - 1;

            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                TextSpan span = _spans[middle];

                if (span.Contains(start))
                {
                    return true;
                }

                if (start < span.Start)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return false;
        }
    }
}
