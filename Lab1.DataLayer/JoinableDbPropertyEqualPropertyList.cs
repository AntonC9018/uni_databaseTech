namespace Lab1.DataLayer;

public readonly struct JoinableDbPropertyEqualVariablePropertyList<TName, TValue>
    where TName : ISpanFormattable
    where TValue : ISpanFormattable

    : ISpanFormattable, IDisposable
{
    private readonly IEnumerator<TName> _namesEnumerator;
    private readonly Func<int, TValue> _valueFunc;
    private readonly bool _isEmpty;
    private int _index;

    public JoinableDbPropertyList(IEnumerable<TName> names, Func<int, TValue> valueFunc)
    {
        _prefix = prefix;
        _namesEnumerator = names.GetEnumerator();
        _isEmpty = !_namesEnumerator.MoveNext();
        _valueFunc = valueFunc;
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
            TValue value = _valueFunc(_index);
            if (_index == 0)
            {
                success = destination.TryWrite(
                    provider,
                    $"{_namesEnumerator.Current} = {value}",
                    out newWritten);
            }
            else
            {
                success = destination.TryWrite(
                    provider,
                    $", {_namesEnumerator.Current} = {value}",
                    out newWritten);
            }

            if (!success)
                return false;

            charsWritten += newWritten;
            destination = destination[newWritten ..];

            if (!_namesEnumerator.MoveNext())
                return true;
            _index++;
        }
    }

    public void Dispose() => _namesEnumerator.Dispose();
}

public static class JoinableHelper
{
    public static JoinableDbPropertyEqualVariablePropertyList<TProperty, TValue> EqualValueList(
        IEnumerable<TProperty> property,
        Func<int, TValue> valueFunc)
    {
        return new(property, valueFunc);
    }
}