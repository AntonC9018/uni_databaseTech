namespace Lab1.DataLayer;

public readonly struct FullyQualifiedName : ISpanFormattable
{
    public readonly string Schema;
    public readonly string Name;

    public FullyQualifiedName(string schema, string name)
    {
        Schema = schema;
        Name = name;
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
        return destination.TryWrite(
            provider,
            $"[{Schema}].[{Name}]",
            out charsWritten);
    }
}