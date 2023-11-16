namespace Lab1.DataLayer;

public struct JoinableDbPropertyEqualVariablePropertyList<TName, TValue>
    : ISpanFormattable, IDisposable

    where TName : ISpanFormattable
    where TValue : ISpanFormattable
{
    private readonly IEnumerator<TName> _namesEnumerator;
    private readonly Func<int, TValue> _valueFunc;
    private readonly string _delimiter;
    private readonly bool IsEmpty => _index == -1;
    private int _index;

    public JoinableDbPropertyEqualVariablePropertyList(
        IEnumerable<TName> names,
        Func<int, TValue> valueFunc,
        string delimiter = ", ")
    {
        _namesEnumerator = names.GetEnumerator();
        _valueFunc = valueFunc;
        _index = !_namesEnumerator.MoveNext() ? -1 : 0;
        _delimiter = delimiter;
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
        if (IsEmpty)
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
                    $"{_delimiter}{_namesEnumerator.Current} = {value}",
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

public static partial class JoinableHelper
{
    public static JoinableDbPropertyEqualVariablePropertyList<TProperty, TValue> 
        EqualValueList<TProperty, TValue>(
            this IEnumerable<TProperty> properties,
            Func<int, TValue> valueFunc,
            string delimiter = ", ")
        where TProperty : ISpanFormattable
        where TValue : ISpanFormattable
    {
        return new(properties, valueFunc);
    }
}