using System;
using System.Collections.Generic;

namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// Turns C# source into tokens. It does not parse: it only has to be exact about what is code
    /// and what is a comment, a string or a directive, so that later steps never match an API call
    /// inside a comment or a literal.
    ///
    /// It never throws and never gives up on the rest of the file. Unclosed constructs produce a
    /// diagnostic and the scan goes on, because the scanner reads inactive #if branches too, where
    /// an apostrophe in prose or a stray quote is perfectly legal.
    /// </summary>
    internal static class CSharpLexer
    {
        internal static LexResult Lex(string source)
        {
            return new Scanner(source ?? string.Empty).Run();
        }

        /// <summary>
        /// One interpolated string being scanned. They nest, so these live on a stack: the top
        /// frame decides whether the scanner is reading literal text or an expression.
        /// </summary>
        private sealed class InterpolationFrame
        {
            /// <summary>Opened with $@" or @$": backslash is not an escape, "" is a quote, newlines are allowed.</summary>
            internal bool IsVerbatim;

            /// <summary>Reading an expression inside { }, as opposed to the literal text around it.</summary>
            internal bool InHole;

            /// <summary>Depth of { } inside the hole. A } at depth zero closes the hole.</summary>
            internal int BraceDepth;

            /// <summary>Depth of ( ) and [ ] inside the hole. A : inside them is an operator, not a format.</summary>
            internal int ParenDepth;

            /// <summary>Where the literal chunk being read started, with its position.</summary>
            internal int ChunkStart;

            internal int ChunkLine;

            internal int ChunkColumn;
        }

        private sealed class Scanner
        {
            private const char ByteOrderMark = '﻿';

            /// <summary>Longest first, so that <c>&lt;&lt;=</c> wins over <c>&lt;=</c>.</summary>
            private static readonly string[] Operators =
            {
                "<<=", ">>=", "??=",
                "==", "!=", "<=", ">=", "=>", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",
                "??", "?.", "++", "--", "&&", "||", "::",
            };

            private readonly string _source;
            private readonly List<Token> _tokens = new List<Token>();
            private readonly List<LexDiagnostic> _diagnostics = new List<LexDiagnostic>();
            private readonly List<int> _lineStarts = new List<int>();
            private readonly List<InterpolationFrame> _frames = new List<InterpolationFrame>();

            private int _position;
            private int _line = 1;
            private int _lineStart;

            internal Scanner(string source)
            {
                _source = source;

                // A leading byte order mark is not part of the text: the first token starts at
                // column 1 either way.
                if (_source.Length > 0 && _source[0] == ByteOrderMark)
                {
                    _position = 1;
                    _lineStart = 1;
                }

                _lineStarts.Add(_lineStart);
            }

            internal LexResult Run()
            {
                try
                {
                    ScanAll();
                }
                catch (Exception)
                {
                    AddDiagnostic(LexDiagnosticCode.InternalError, _line, CurrentColumn());
                }

                return new LexResult(_source, _tokens, _diagnostics, _lineStarts);
            }

            private void ScanAll()
            {
                while (_position < _source.Length)
                {
                    int positionBefore = _position;
                    int tokensBefore = _tokens.Count;
                    int diagnosticsBefore = _diagnostics.Count;
                    int framesBefore = _frames.Count;

                    InterpolationFrame frame = CurrentFrame();
                    if (frame != null && !frame.InHole)
                    {
                        ScanInterpolationText(frame);
                    }
                    else
                    {
                        ScanCodeToken();
                    }

                    bool madeProgress = _position != positionBefore ||
                                        _tokens.Count != tokensBefore ||
                                        _diagnostics.Count != diagnosticsBefore ||
                                        _frames.Count != framesBefore;
                    if (!madeProgress)
                    {
                        // Cannot happen by design; step over the character rather than spin.
                        _position++;
                    }
                }

                // The file ended inside the hole of an interpolated string.
                while (_frames.Count > 0)
                {
                    InterpolationFrame unfinished = _frames[_frames.Count - 1];
                    _frames.RemoveAt(_frames.Count - 1);
                    AddDiagnostic(UnterminatedCodeOf(unfinished), unfinished.ChunkLine, unfinished.ChunkColumn);
                }
            }

            private void ScanCodeToken()
            {
                SkipWhitespaceAndNewLines();
                if (_position >= _source.Length)
                {
                    return;
                }

                char c = _source[_position];

                if (c == '/' && Peek(1) == '/')
                {
                    SkipLineComment();
                    return;
                }

                if (c == '/' && Peek(1) == '*')
                {
                    SkipBlockComment();
                    return;
                }

                // Inside an interpolation hole a # is an ordinary character: a directive can only
                // start a line of real code.
                if (c == '#' && _frames.Count == 0 && AtFirstNonWhitespaceOfLine())
                {
                    ScanDirective();
                    return;
                }

                if (IsIdentifierStart(c))
                {
                    ScanIdentifier();
                    return;
                }

                if (c == '@')
                {
                    if (Peek(1) == '"')
                    {
                        ScanVerbatimString();
                        return;
                    }

                    if (Peek(1) == '$' && Peek(2) == '"')
                    {
                        OpenInterpolatedString(true, 3);
                        return;
                    }

                    if (IsIdentifierStart(Peek(1)))
                    {
                        ScanVerbatimIdentifier();
                        return;
                    }

                    ScanPunct();
                    return;
                }

                if (c == '$')
                {
                    if (Peek(1) == '"')
                    {
                        OpenInterpolatedString(false, 2);
                        return;
                    }

                    if (Peek(1) == '@' && Peek(2) == '"')
                    {
                        OpenInterpolatedString(true, 3);
                        return;
                    }

                    ScanPunct();
                    return;
                }

                if (c == '"')
                {
                    ScanRegularString();
                    return;
                }

                if (c == '\'')
                {
                    ScanCharLiteral();
                    return;
                }

                if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek(1))))
                {
                    ScanNumber();
                    return;
                }

                ScanPunct();
            }

            private void ScanIdentifier()
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                while (_position < _source.Length && IsIdentifierPart(_source[_position]))
                {
                    _position++;
                }

                AddToken(TokenKind.Identifier, _source.Substring(start, _position - start), line, column, start);
            }

            /// <summary>@class is the identifier class: the @ only keeps the keyword out of the way.</summary>
            private void ScanVerbatimIdentifier()
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                _position++;
                int nameStart = _position;
                while (_position < _source.Length && IsIdentifierPart(_source[_position]))
                {
                    _position++;
                }

                _tokens.Add(new Token(
                    TokenKind.Identifier,
                    _source.Substring(nameStart, _position - nameStart),
                    line,
                    column,
                    start,
                    _position - start));
            }

            private void ScanNumber()
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                bool hexadecimal = _source[_position] == '0' && (Peek(1) == 'x' || Peek(1) == 'X');
                bool seenDot = false;

                while (_position < _source.Length)
                {
                    char c = _source[_position];

                    if (!hexadecimal && (c == 'e' || c == 'E') && (Peek(1) == '+' || Peek(1) == '-'))
                    {
                        _position += 2;
                        continue;
                    }

                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        _position++;
                        continue;
                    }

                    // The dot is part of the number only in front of a digit, so that
                    // Time.timeScale stays three tokens.
                    if (c == '.' && !seenDot && !hexadecimal && char.IsDigit(Peek(1)))
                    {
                        seenDot = true;
                        _position++;
                        continue;
                    }

                    break;
                }

                AddToken(TokenKind.Number, _source.Substring(start, _position - start), line, column, start);
            }

            private void ScanRegularString()
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                _position++;
                while (_position < _source.Length)
                {
                    char c = _source[_position];

                    if (c == '\\' && !IsNewLine(Peek(1)) && _position + 1 < _source.Length)
                    {
                        _position += 2;
                        continue;
                    }

                    if (c == '"')
                    {
                        _position++;
                        AddToken(TokenKind.String, _source.Substring(start, _position - start), line, column, start);
                        return;
                    }

                    if (IsNewLine(c))
                    {
                        break;
                    }

                    _position++;
                }

                // Unterminated: the token ends with the line, so the rest of the file still reads
                // as code.
                AddToken(TokenKind.String, _source.Substring(start, _position - start), line, column, start);
                AddDiagnostic(LexDiagnosticCode.UnterminatedString, line, column);
                PopToVerbatimFrame();
            }

            private void ScanVerbatimString()
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                _position += 2;
                while (_position < _source.Length)
                {
                    char c = _source[_position];

                    if (c == '"')
                    {
                        if (Peek(1) == '"')
                        {
                            _position += 2;
                            continue;
                        }

                        _position++;
                        AddToken(TokenKind.String, _source.Substring(start, _position - start), line, column, start);
                        return;
                    }

                    if (IsNewLine(c))
                    {
                        AdvanceNewLine();
                        continue;
                    }

                    _position++;
                }

                AddToken(TokenKind.String, _source.Substring(start, _position - start), line, column, start);
                AddDiagnostic(LexDiagnosticCode.UnterminatedVerbatimString, line, column);
            }

            private void ScanCharLiteral()
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                _position++;
                while (_position < _source.Length)
                {
                    char c = _source[_position];

                    if (c == '\\' && !IsNewLine(Peek(1)) && _position + 1 < _source.Length)
                    {
                        _position += 2;
                        continue;
                    }

                    if (c == '\'')
                    {
                        _position++;
                        AddToken(TokenKind.Char, _source.Substring(start, _position - start), line, column, start);
                        return;
                    }

                    if (IsNewLine(c))
                    {
                        break;
                    }

                    _position++;
                }

                // No closing quote on this line: it was an apostrophe in prose, not a literal.
                // Report it, emit the quote itself and read on from the next character.
                _position = start + 1;
                AddToken(TokenKind.Punct, "'", line, column, start);
                AddDiagnostic(LexDiagnosticCode.UnterminatedCharLiteral, line, column);
            }

            private void OpenInterpolatedString(bool verbatim, int prefixLength)
            {
                var frame = new InterpolationFrame
                {
                    IsVerbatim = verbatim,
                    InHole = false,
                    ChunkStart = _position,
                    ChunkLine = _line,
                    ChunkColumn = CurrentColumn(),
                };

                _position += prefixLength;
                _frames.Add(frame);
            }

            /// <summary>
            /// The literal part of an interpolated string. The opening delimiter belongs to the
            /// first chunk and the closing quote to the last one, so every emitted chunk is
            /// non-empty; chunks between two holes may be empty and are not emitted.
            /// </summary>
            private void ScanInterpolationText(InterpolationFrame frame)
            {
                while (_position < _source.Length)
                {
                    char c = _source[_position];

                    if (c == '{')
                    {
                        if (Peek(1) == '{')
                        {
                            _position += 2;
                            continue;
                        }

                        EmitChunk(frame, _position);
                        AddToken(TokenKind.Punct, "{", _line, CurrentColumn(), _position);
                        _position++;
                        frame.InHole = true;
                        frame.BraceDepth = 0;
                        frame.ParenDepth = 0;
                        return;
                    }

                    if (c == '}')
                    {
                        // }} is an escape; a lone } here is invalid C# but still just text.
                        _position += Peek(1) == '}' ? 2 : 1;
                        continue;
                    }

                    if (c == '"')
                    {
                        if (frame.IsVerbatim && Peek(1) == '"')
                        {
                            _position += 2;
                            continue;
                        }

                        _position++;
                        EmitChunk(frame, _position);
                        PopFrame();
                        return;
                    }

                    if (!frame.IsVerbatim && c == '\\' && !IsNewLine(Peek(1)) && _position + 1 < _source.Length)
                    {
                        _position += 2;
                        continue;
                    }

                    if (IsNewLine(c))
                    {
                        if (frame.IsVerbatim)
                        {
                            AdvanceNewLine();
                            continue;
                        }

                        EmitChunk(frame, _position);
                        AddDiagnostic(LexDiagnosticCode.UnterminatedString, frame.ChunkLine, frame.ChunkColumn);
                        PopToVerbatimFrame();
                        return;
                    }

                    _position++;
                }

                EmitChunk(frame, _position);
                AddDiagnostic(UnterminatedCodeOf(frame), frame.ChunkLine, frame.ChunkColumn);
                _frames.Clear();
            }

            /// <summary>
            /// A verbatim string that never closed swallowed the rest of the file; a plain one
            /// only cost the line it started on.
            /// </summary>
            private static LexDiagnosticCode UnterminatedCodeOf(InterpolationFrame frame)
            {
                return frame.IsVerbatim
                    ? LexDiagnosticCode.UnterminatedVerbatimString
                    : LexDiagnosticCode.UnterminatedString;
            }

            /// <summary>Everything after a top level : up to the closing brace is format, not code.</summary>
            private void ScanFormatSpec(InterpolationFrame frame)
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                while (_position < _source.Length)
                {
                    char c = _source[_position];

                    if (c == '}')
                    {
                        break;
                    }

                    if (IsNewLine(c))
                    {
                        if (frame.IsVerbatim)
                        {
                            AdvanceNewLine();
                            continue;
                        }

                        EmitText(TokenKind.String, start, _position, line, column);
                        AddDiagnostic(LexDiagnosticCode.UnterminatedString, frame.ChunkLine, frame.ChunkColumn);
                        PopToVerbatimFrame();
                        return;
                    }

                    _position++;
                }

                EmitText(TokenKind.String, start, _position, line, column);
            }

            private void ScanPunct()
            {
                InterpolationFrame frame = CurrentFrame();
                bool inHole = frame != null && frame.InHole;
                char c = _source[_position];
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                if (inHole && c == '}' && frame.BraceDepth == 0)
                {
                    AddToken(TokenKind.Punct, "}", line, column, start);
                    _position++;
                    frame.InHole = false;
                    frame.BraceDepth = 0;
                    frame.ParenDepth = 0;
                    frame.ChunkStart = _position;
                    frame.ChunkLine = _line;
                    frame.ChunkColumn = CurrentColumn();
                    return;
                }

                if (inHole && c == ':' && frame.BraceDepth == 0 && frame.ParenDepth == 0 && Peek(1) != ':')
                {
                    AddToken(TokenKind.Punct, ":", line, column, start);
                    _position++;
                    ScanFormatSpec(frame);
                    return;
                }

                string op = MatchOperator();
                if (op != null)
                {
                    AddToken(TokenKind.Punct, op, line, column, start);
                    _position += op.Length;
                    return;
                }

                AddToken(TokenKind.Punct, c.ToString(), line, column, start);
                _position++;

                if (!inHole)
                {
                    return;
                }

                if (c == '{')
                {
                    frame.BraceDepth++;
                }
                else if (c == '}' && frame.BraceDepth > 0)
                {
                    frame.BraceDepth--;
                }
                else if (c == '(' || c == '[')
                {
                    frame.ParenDepth++;
                }
                else if ((c == ')' || c == ']') && frame.ParenDepth > 0)
                {
                    frame.ParenDepth--;
                }
            }

            private string MatchOperator()
            {
                foreach (string op in Operators)
                {
                    if (!MatchesAt(op))
                    {
                        continue;
                    }

                    // x ? .5f : 1f is a conditional over a number, not a null conditional access.
                    if (op == "?." && char.IsDigit(Peek(2)))
                    {
                        continue;
                    }

                    return op;
                }

                return null;
            }

            private void ScanDirective()
            {
                int start = _position;
                int line = _line;
                int column = CurrentColumn();

                int end = _position;
                while (end < _source.Length && !IsNewLine(_source[end]))
                {
                    end++;
                }

                while (end > start && char.IsWhiteSpace(_source[end - 1]))
                {
                    end--;
                }

                AddToken(TokenKind.Directive, _source.Substring(start, end - start), line, column, start);
                _position = end;
            }

            private void SkipLineComment()
            {
                while (_position < _source.Length && !IsNewLine(_source[_position]))
                {
                    _position++;
                }
            }

            private void SkipBlockComment()
            {
                int line = _line;
                int column = CurrentColumn();

                _position += 2;
                while (_position < _source.Length)
                {
                    if (_source[_position] == '*' && Peek(1) == '/')
                    {
                        _position += 2;
                        return;
                    }

                    if (IsNewLine(_source[_position]))
                    {
                        AdvanceNewLine();
                        continue;
                    }

                    _position++;
                }

                AddDiagnostic(LexDiagnosticCode.UnterminatedComment, line, column);
            }

            private void SkipWhitespaceAndNewLines()
            {
                while (_position < _source.Length)
                {
                    char c = _source[_position];
                    if (IsNewLine(c))
                    {
                        AdvanceNewLine();
                        continue;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        _position++;
                        continue;
                    }

                    break;
                }
            }

            private void AdvanceNewLine()
            {
                char c = _source[_position];
                _position++;
                if (c == '\r' && _position < _source.Length && _source[_position] == '\n')
                {
                    _position++;
                }

                _line++;
                _lineStart = _position;
                _lineStarts.Add(_lineStart);
            }

            private bool AtFirstNonWhitespaceOfLine()
            {
                for (int i = _lineStart; i < _position; i++)
                {
                    if (!char.IsWhiteSpace(_source[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            private InterpolationFrame CurrentFrame()
            {
                return _frames.Count == 0 ? null : _frames[_frames.Count - 1];
            }

            private void PopFrame()
            {
                if (_frames.Count > 0)
                {
                    _frames.RemoveAt(_frames.Count - 1);
                }
            }

            /// <summary>
            /// A literal that a line break cut short breaks every interpolation around it that
            /// cannot span lines either. Verbatim ones can, so the unwinding stops there.
            /// </summary>
            private void PopToVerbatimFrame()
            {
                while (_frames.Count > 0 && !_frames[_frames.Count - 1].IsVerbatim)
                {
                    _frames.RemoveAt(_frames.Count - 1);
                }
            }

            private void EmitChunk(InterpolationFrame frame, int end)
            {
                EmitText(TokenKind.String, frame.ChunkStart, end, frame.ChunkLine, frame.ChunkColumn);
            }

            private void EmitText(TokenKind kind, int start, int end, int line, int column)
            {
                if (end > start)
                {
                    AddToken(kind, _source.Substring(start, end - start), line, column, start);
                }
            }

            private void AddToken(TokenKind kind, string text, int line, int column, int start)
            {
                _tokens.Add(new Token(kind, text, line, column, start, text.Length));
            }

            private void AddDiagnostic(LexDiagnosticCode code, int line, int column)
            {
                _diagnostics.Add(new LexDiagnostic(code, line, column));
            }

            private int CurrentColumn()
            {
                return _position - _lineStart + 1;
            }

            private char Peek(int offset)
            {
                int index = _position + offset;
                return index < _source.Length ? _source[index] : '\0';
            }

            private bool MatchesAt(string text)
            {
                if (_position + text.Length > _source.Length)
                {
                    return false;
                }

                for (int i = 0; i < text.Length; i++)
                {
                    if (_source[_position + i] != text[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool IsNewLine(char c)
            {
                return c == '\n' || c == '\r';
            }

            private static bool IsIdentifierStart(char c)
            {
                return char.IsLetter(c) || c == '_';
            }

            private static bool IsIdentifierPart(char c)
            {
                return char.IsLetterOrDigit(c) || c == '_';
            }
        }
    }
}
