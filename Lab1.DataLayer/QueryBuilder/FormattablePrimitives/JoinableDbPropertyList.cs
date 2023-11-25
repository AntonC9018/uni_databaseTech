namespace Lab1.DataLayer;

public readonly struct JoinableDbPropertyList<T>
    where T : ISpanFormattable
{
    private readonly IEnumerable<T> _names;

    public JoinableDbPropertyList(IEnumerable<T> names)
    {
        _names = names;
    }

    public FormattableListState Prefix(string? prefix) => new(prefix, this);


    public struct FormattableListState : ISpanFormattable, IDisposable
    {
        private readonly string? _prefix;
        private readonly IEnumerator<T> _namesEnumerator;
        private readonly bool _isEmpty;
        private bool _isFirst;

        internal FormattableListState(
            string? prefix,
            JoinableDbPropertyList<T> joinableDbPropertyList)
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

                if (!_isFirst)
                {
                    success = destination.TryWrite(
                        provider,
                        $", ",
                        out newWritten);
                    if (!success)
                        return false;

                    charsWritten += newWritten;
                    destination = destination[newWritten ..];
                }

                if (_prefix is not null)
                {
                    success = destination.TryWrite(
                        provider,
                        $"{_prefix}.",
                        out newWritten);

                    if (!success)
                        return false;

                    charsWritten += newWritten;
                    destination = destination[newWritten ..];
                }

                var current = _namesEnumerator.Current;
                if (current is IRequiresSquareBrackets { RequiresSquareBrackets: false })
                {
                    success = destination.TryWrite(
                        provider,
                        $"{current}",
                        out newWritten);

                    if (!success)
                        return false;

                    charsWritten += newWritten;
                    destination = destination[newWritten ..];
                }
                else
                {
                    success = destination.TryWrite(
                        provider,
                        $"[{current}]",
                        out newWritten);

                    if (!success)
                        return false;

                    charsWritten += newWritten;
                    destination = destination[newWritten ..];
                }

                _isFirst = false;

                if (!_namesEnumerator.MoveNext())
                    return true;
            }
        }

        public void Dispose() => _namesEnumerator.Dispose();
    }
}

public readonly struct ValueAsFormattable : ISpanFormattable, IRequiresSquareBrackets
{
    private readonly string _value;
    private readonly bool _needsSquareBrackets;

    public ValueAsFormattable(string value, bool needsSquareBrackets)
    {
        _value = value;
        _needsSquareBrackets = needsSquareBrackets;
    }

    public bool RequiresSquareBrackets => _needsSquareBrackets;

    public string ToString(
        string? format,
        IFormatProvider? formatProvider)
    {
        return _value;
    }

    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return destination.TryWrite(
            provider,
            $"{_value}",
            out charsWritten);
    }
}

public static partial class JoinableHelper
{
    public static JoinableDbPropertyList<T> JoinableDbPropertyList<T>(
            this IEnumerable<T> properties)

        where T : ISpanFormattable
    {
        return new(properties);
    }

    public static JoinableDbPropertyList<ValueAsFormattable> JoinableDbPropertyList(
        this IEnumerable<string> properties,
        bool needsSquareBrackets = true)
    {
        return new(properties.Select(x => new ValueAsFormattable(x, needsSquareBrackets)));
    }
}