namespace Lab1.DataLayer;

public readonly struct JoinableDbPropertyList<T>
    where T : ISpanFormattable
{
    private readonly IEnumerable<T> _names;

    public JoinableDbPropertyList(IEnumerable<T> names)
    {
        _names = names;
    }

    public FormattableList Prefix(string? prefix) => new(prefix, this);


    public readonly struct FormattableList : ISpanFormattable
    {
        private readonly string? _prefix;
        private readonly IEnumerable<T> _namesEnumerator;

        internal FormattableList(
            string? prefix,
            JoinableDbPropertyList<T> joinableDbPropertyList)
        {
            _prefix = prefix;
            _namesEnumerator = joinableDbPropertyList._names;
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

            using var e = _namesEnumerator.GetEnumerator();
            bool isFirst = true;

            while (e.MoveNext())
            {
                bool success;
                int newWritten;
                int charsWrittenLocal = 0;

                if (!isFirst)
                {
                    success = destination.TryWrite(
                        provider,
                        $", ",
                        out newWritten);
                    if (!success)
                        return false;

                    charsWrittenLocal += newWritten;
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

                    charsWrittenLocal += newWritten;
                    destination = destination[newWritten ..];
                }

                var current = e.Current;
                if (current is IRequiresSquareBrackets { RequiresSquareBrackets: false })
                {
                    success = destination.TryWrite(
                        provider,
                        $"{current}",
                        out newWritten);

                    if (!success)
                        return false;

                    charsWrittenLocal += newWritten;
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

                    charsWrittenLocal += newWritten;
                    destination = destination[newWritten ..];
                }

                isFirst = false;
                charsWritten += charsWrittenLocal;
            }
            return true;
        }
    }
}

public readonly struct ValueAsFormattable : ISpanFormattable, IRequiresSquareBrackets
{
    private readonly string _value;
    private readonly bool _needsSquareBrackets;

    public ValueAsFormattable(string value, bool needsSquareBrackets = true)
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