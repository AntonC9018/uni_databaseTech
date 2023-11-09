namespace Lab1.DataLayer;

public readonly struct JoinableDbPropertyList
{
    private readonly IEnumerable<string> _names;

    public JoinableDbPropertyList(IEnumerable<string> names)
    {
        _names = names;
    }

    public FormattableListState Prefix(string prefix) => new(prefix, this);


    public struct FormattableListState : ISpanFormattable, IDisposable
    {
        private readonly string _prefix;
        private readonly IEnumerator<string> _namesEnumerator;
        private readonly bool _isEmpty;
        private bool _isFirst;

        internal FormattableListState(string prefix, JoinableDbPropertyList joinableDbPropertyList)
        {
            _prefix = prefix;
            _namesEnumerator = joinableDbPropertyList._names.GetEnumerator();
            _isEmpty = !_namesEnumerator.MoveNext();
            _isFirst = true;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            throw new NotImplementedException();
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            charsWritten = 0;
            if (_isEmpty)
                return true;

            while (true)
            {
                bool success;
                int newWritten;
                if (_isFirst)
                {
                    success = destination.TryWrite(
                        provider,
                        $"{_prefix}.[{_namesEnumerator.Current}]",
                        out newWritten);
                }
                else
                {
                    success = destination.TryWrite(
                        provider,
                        $", {_prefix}.[{_namesEnumerator.Current}]",
                        out newWritten);
                }

                if (!success)
                    return false;

                _isFirst = false;
                charsWritten += newWritten;
                destination = destination[newWritten ..];

                if (!_namesEnumerator.MoveNext())
                    return true;
            }
        }

        public void Dispose() => _namesEnumerator.Dispose();
    }
}