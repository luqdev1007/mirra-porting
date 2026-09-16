using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Walks the directives of a file and works out which branches the player build drops.
    ///
    /// A branch counts as editor only when its effective condition is definitely False: the
    /// condition of #if, the condition of #elif with every earlier condition negated, or the
    /// negation of all of them for #else. Anything inside an editor only branch is editor only
    /// too, whatever its own condition says.
    /// </summary>
    internal static class ConditionalRegionBuilder
    {
        internal static ConditionalRegions Build(LexResult lex)
        {
            if (lex == null)
            {
                return ConditionalRegions.Empty;
            }

            var spans = new List<TextSpan>();
            var diagnostics = new List<LexDiagnostic>();
            var blocks = new List<Block>();

            try
            {
                Walk(lex, spans, diagnostics, blocks);
            }
            catch (Exception)
            {
                diagnostics.Add(new LexDiagnostic(LexDiagnosticCode.InternalError, 1, 1));
            }

            return new ConditionalRegions(Merge(spans), diagnostics);
        }

        private static void Walk(LexResult lex, List<TextSpan> spans, List<LexDiagnostic> diagnostics, List<Block> blocks)
        {
            foreach (Token token in lex.Tokens)
            {
                if (token.Kind != TokenKind.Directive)
                {
                    continue;
                }

                DirectiveLine directive = DirectiveLine.Parse(token);
                int bodyStart = token.Start + token.Length;

                switch (directive.Kind)
                {
                    case DirectiveKind.If:
                        OpenBlock(blocks, token, EvaluateCondition(directive, token, diagnostics), bodyStart);
                        break;

                    case DirectiveKind.Elif:
                        ContinueBlock(blocks, spans, diagnostics, token, bodyStart, EvaluateCondition(directive, token, diagnostics), false);
                        break;

                    case DirectiveKind.Else:
                        ContinueBlock(blocks, spans, diagnostics, token, bodyStart, ConditionValue.Unknown, true);
                        break;

                    case DirectiveKind.EndIf:
                        CloseBlock(blocks, spans, diagnostics, token);
                        break;
                }
            }

            // Whatever is still open runs to the end of the file.
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                Block block = blocks[i];
                CloseBranch(block, spans, lex.SourceLength);
                diagnostics.Add(new LexDiagnostic(LexDiagnosticCode.UnclosedConditional, block.OpenLine, block.OpenColumn));
            }

            blocks.Clear();
        }

        private static ConditionValue EvaluateCondition(DirectiveLine directive, Token token, List<LexDiagnostic> diagnostics)
        {
            ConditionValue value;
            if (ConditionEvaluator.TryEvaluate(directive.Argument, out value))
            {
                return value;
            }

            diagnostics.Add(new LexDiagnostic(LexDiagnosticCode.InvalidConditionExpression, token.Line, token.Column));
            return ConditionValue.Unknown;
        }

        private static void OpenBlock(List<Block> blocks, Token token, ConditionValue condition, int bodyStart)
        {
            bool parentEditorOnly = blocks.Count > 0 && IsBranchEditorOnly(blocks[blocks.Count - 1]);

            blocks.Add(new Block
            {
                PreviousOr = condition,
                Effective = condition,
                BodyStart = bodyStart,
                ParentEditorOnly = parentEditorOnly,
                OpenLine = token.Line,
                OpenColumn = token.Column,
            });
        }

        private static void ContinueBlock(
            List<Block> blocks,
            List<TextSpan> spans,
            List<LexDiagnostic> diagnostics,
            Token token,
            int bodyStart,
            ConditionValue condition,
            bool isElse)
        {
            if (blocks.Count == 0)
            {
                diagnostics.Add(new LexDiagnostic(LexDiagnosticCode.UnbalancedDirective, token.Line, token.Column));
                return;
            }

            Block block = blocks[blocks.Count - 1];
            CloseBranch(block, spans, token.Start);

            if (block.SeenElse)
            {
                // #elif or a second #else after #else: report it and keep going on the same block.
                diagnostics.Add(new LexDiagnostic(LexDiagnosticCode.UnbalancedDirective, token.Line, token.Column));
            }

            ConditionValue effective = isElse
                ? ConditionLogic.Not(block.PreviousOr)
                : ConditionLogic.And(condition, ConditionLogic.Not(block.PreviousOr));

            block.Effective = effective;
            block.PreviousOr = ConditionLogic.Or(block.PreviousOr, isElse ? effective : condition);
            block.SeenElse = block.SeenElse || isElse;
            block.BodyStart = bodyStart;
        }

        private static void CloseBlock(List<Block> blocks, List<TextSpan> spans, List<LexDiagnostic> diagnostics, Token token)
        {
            if (blocks.Count == 0)
            {
                diagnostics.Add(new LexDiagnostic(LexDiagnosticCode.UnbalancedDirective, token.Line, token.Column));
                return;
            }

            Block block = blocks[blocks.Count - 1];
            blocks.RemoveAt(blocks.Count - 1);
            CloseBranch(block, spans, token.Start);
        }

        private static void CloseBranch(Block block, List<TextSpan> spans, int end)
        {
            if (IsBranchEditorOnly(block) && end > block.BodyStart)
            {
                spans.Add(new TextSpan(block.BodyStart, end));
            }
        }

        private static bool IsBranchEditorOnly(Block block)
        {
            return block.ParentEditorOnly || block.Effective == ConditionValue.False;
        }

        /// <summary>Sorts and joins spans that touch or overlap, so a nested branch folds into its parent.</summary>
        private static IReadOnlyList<TextSpan> Merge(List<TextSpan> spans)
        {
            if (spans.Count == 0)
            {
                return new TextSpan[0];
            }

            spans.Sort((left, right) => left.Start.CompareTo(right.Start));

            var merged = new List<TextSpan>();
            TextSpan current = spans[0];

            for (int i = 1; i < spans.Count; i++)
            {
                TextSpan next = spans[i];
                if (next.Start <= current.End)
                {
                    if (next.End > current.End)
                    {
                        current = new TextSpan(current.Start, next.End);
                    }

                    continue;
                }

                merged.Add(current);
                current = next;
            }

            merged.Add(current);
            return merged;
        }

        /// <summary>One #if block being walked, from its #if to its #endif.</summary>
        private sealed class Block
        {
            /// <summary>Every condition seen so far in this block, ored together.</summary>
            internal ConditionValue PreviousOr;

            /// <summary>Effective condition of the branch being walked right now.</summary>
            internal ConditionValue Effective;

            /// <summary>Offset right after the directive line that opened the current branch.</summary>
            internal int BodyStart;

            internal bool ParentEditorOnly;

            internal bool SeenElse;

            internal int OpenLine;

            internal int OpenColumn;
        }
    }
}
