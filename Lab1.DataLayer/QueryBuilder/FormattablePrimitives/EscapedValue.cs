using System.Diagnostics;
using System.Security.Cryptography;

namespace Lab1.DataLayer;

public readonly struct EscapedValue : ISpanFormattable
{
    private readonly string _value;

    public EscapedValue(string value)
    {
        _value = value;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        throw new NotImplementedException();
    }

    private ref struct Locals
    {
        public required Span<char> Destination;
        public required int CharsWritten;
        public required IFormatProvider? Provider;

        public void Move(int i = 1)
        {
            Debug.Assert(i <= Destination.Length);
            CharsWritten += i;
            Destination = Destination[i..];
        }
    }

    private bool TryFormat(ref Locals locals)
    {
        static bool WriteSingleChar(char ch, ref Locals locals)
        {
            if (locals.Destination.Length < 1)
                return false;
            
            locals.Destination[0] = ch;
            locals.Move();
            return true;
        }

        {
            if (!WriteSingleChar('\'', ref locals))
                return false;
        }
        int position = 0;

        while (position < _value.Length)
        {
            char ch = _value[position];
            if (ch != '\'')
            {
                if (!WriteSingleChar('\'', ref locals))
                    return false;
            }
            else
            {
                bool success = locals.Destination.TryWrite(
                    locals.Provider,
                    $"''",
                    out _);
                if (!success)
                    return false;
                locals.Move(2);
            }
        }

        // if (_position == _value.Length)
        {
            if (!WriteSingleChar('\'', ref locals))
                return false;
        }

        return true;
    }

    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        Locals locals;
        locals.Destination = destination;
        locals.CharsWritten = 0;
        locals.Provider = provider;
        bool written = TryFormat(ref locals);
        charsWritten = locals.CharsWritten;
        return written;
    }
}