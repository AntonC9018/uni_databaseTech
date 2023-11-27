namespace Lab1.DataLayer;

public readonly struct JoinableDbPropertyEqualVariablePropertyList<TName, TValue>
    : ISpanFormattable

    where TName : ISpanFormattable
    where TValue : ISpanFormattable
{
    private readonly IEnumerable<TName> _namesEnumerator;
    private readonly Func<int, TValue> _valueFunc;
    private readonly string _delimiter;

    public JoinableDbPropertyEqualVariablePropertyList(
        IEnumerable<TName> names,
        Func<int, TValue> valueFunc,
        string delimiter = ", ")
    {
        _namesEnumerator = names;
        _valueFunc = valueFunc;
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
        using var e = _namesEnumerator.GetEnumerator();
        if (!e.MoveNext())
            return true;

        int index = 0;

        while (true)
        {
            bool success;
            int newWritten;
            TValue value = _valueFunc(index);
            if (index == 0)
            {
                success = destination.TryWrite(
                    provider,
                    $"{e.Current} = {value}",
                    out newWritten);
            }
            else
            {
                success = destination.TryWrite(
                    provider,
                    $"{_delimiter}{e.Current} = {value}",
                    out newWritten);
            }

            if (!success)
                return false;

            charsWritten += newWritten;
            destination = destination[newWritten ..];

            if (!e.MoveNext())
                return true;
            index++;
        }
    }
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
        return new(properties, valueFunc, delimiter);
    }
}