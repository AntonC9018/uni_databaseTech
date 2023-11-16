namespace Lab1.DataLayer;

public struct EscapedValue : ISpanFormattable
{
    private readonly string _value;
    private int _position;

    public EscapedValue(string value)
    {
        _value = value;
        _position = -1;
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

        bool WriteSingleChar(char ch)
        {
            if (destination.Length < 1)
                return false;
            
            destination[0] = '\'';
            destination = destination[1 ..];
            charsWritten++;
            return true;
        }

        if (_position == -1)
        {
            if (!WriteSingleChar('\''))
                return false;
        }

        while (_position < _value.Length)
        {
            char ch = _value[_position];
            if (ch != '\'')
            {
                if (!WriteSingleChar('\''))
                    return false;
            }
            else
            {
                bool success = destination.TryWrite(
                    provider,
                    $"''",
                    out _);
                if (!success)
                    return false;
            }
        }

        // if (_position == _value.Length)
        {
            if (!WriteSingleChar('\''))
                return false;
        }

        return true;
    }
}