using System.Runtime.CompilerServices;

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

    public string ToString(string? format = null, IFormatProvider? formatProvider = null)
    {
        var handler = new DefaultInterpolatedStringHandler(
            literalLength: 0,
            formattedCount: 1,
            formatProvider);
        handler.AppendFormatted(this, format);
        return handler.ToStringAndClear();
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