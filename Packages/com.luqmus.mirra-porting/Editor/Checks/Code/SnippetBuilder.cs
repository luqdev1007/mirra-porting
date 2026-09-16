using System.Text;
using Luqmus.MirraPorting.Code;

namespace Luqmus.MirraPorting.Checks
{
    /// <summary>
    /// Builds the line of source shown next to a finding. A chain broken over several lines is
    /// glued back together, so that the report shows the whole statement and not its first word.
    /// </summary>
    internal static class SnippetBuilder
    {
        internal static string Build(LexResult lex, int firstLine, int lastLine)
        {
            if (lex == null)
            {
                return string.Empty;
            }

            int from = firstLine < lastLine ? firstLine : lastLine;
            int to = firstLine < lastLine ? lastLine : firstLine;

            var text = new StringBuilder();
            for (int line = from; line <= to; line++)
            {
                string source = lex.GetLineText(line).Trim();
                if (source.Length == 0)
                {
                    continue;
                }

                if (text.Length > 0)
                {
                    text.Append(' ');
                }

                text.Append(source);
            }

            return text.ToString();
        }
    }
}
