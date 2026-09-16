namespace Luqmus.MirraPorting.Code
{
    /// <summary>A half open range of the source: Start is inside, End is not.</summary>
    internal readonly struct TextSpan
    {
        internal TextSpan(int start, int end)
        {
            Start = start;
            End = end;
        }

        internal int Start { get; }

        internal int End { get; }

        internal int Length
        {
            get { return End - Start; }
        }

        internal bool Contains(int offset)
        {
            return offset >= Start && offset < End;
        }

        public override string ToString()
        {
            return "[" + Start + ", " + End + ")";
        }
    }
}
