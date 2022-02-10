using System.Numerics;
using System.Runtime.CompilerServices;

namespace Atom.Engine;

public struct UInt128 : IFormattable, IEquatable<UInt128>//, IComparable<UInt128>
{
    public static readonly u128 MinValue = 0x0;
    public static readonly u128 MaxValue = new (ulong.MaxValue, ulong.MaxValue);

    private const int SIZE_OF = sizeof(ulong) * 2;
    private const int SIZE_OF_BIT = SIZE_OF * 8;
    
    private ulong _a;
    private ulong _b;

    public UInt128(ReadOnlySpan<ulong> longs) => (_a, _b) = (longs[0], longs[1]);
    public UInt128(ulong high, ulong low) => (_a, _b) = (high, low);
    
    
    public static u128 operator >>(u128 n, int shift)
    {
        shift %= sizeof(ulong) * 8 * 2;
        return shift > sizeof(ulong) * 8 
            ? new u128(0, n._a >> shift)
            : new u128(n._a >> shift, (n._b >> shift) | (n._a << (sizeof(ulong) * 8 - shift)));
    }
    
    public static u128 operator <<(u128 n, int shift)
    {
        shift %= sizeof(ulong) * 8 * 2;
        return shift > sizeof(ulong) * 8 
            ? new u128(n._b << shift, 0) 
            : new u128((n._a << shift) | (n._b >> (sizeof(ulong) * 8 - shift)), n._b << shift);
    }

    public static u128 operator ~(u128 n) => new(~n._a, ~n._b);

    public static u128 operator ^(u128 lhs, u128 rhs) => new(lhs._a ^ rhs._a, lhs._b ^ rhs._b);

    public static u128 operator &(u128 lhs, u128 rhs) => new(lhs._a & rhs._a, lhs._b & rhs._b);

    public static u128 operator |(u128 lhs, u128 rhs) => new(lhs._a | rhs._a, lhs._b | rhs._b);


    public static implicit operator u128(in ulong @ulong) => new(0, @ulong);


    public bool Equals(u128 other) => other._a == _a && other._b == _b;

    public override int GetHashCode()
    {
        return HashCode.Combine(_a, _b);
    }

    public string ToString() => ToString("X");
    public string ToString(string? format) => ToString(format, null);
    public string ToString(IFormatProvider? formatProvider) => ToString(null, formatProvider);
    public string ToString(string? format, IFormatProvider? formatProvider) => format switch
    {
        "X" => GetBinaryString(),
        _ => ToString()
    };

    private string GetBinaryString()
    {
        const int ZERO_CHAR_INDEX = '0';
        
        Span<char> buffer = stackalloc char[128];
        for (int i = 0; i < sizeof(ulong)*8; i++)
        {// _a (high)
            buffer[i] = (char)(ZERO_CHAR_INDEX + ((_a >> (sizeof(ulong) * 8 - i - 1)) & 0b1UL));
        }
        for (int i = 0; i < sizeof(ulong)*8; i++)
        {// _b (low)
            buffer[i + sizeof(ulong)*8] = (char)(ZERO_CHAR_INDEX + ((_b >> (sizeof(ulong) * 8 - i - 1)) & 0b1UL));
        }

        return new string(buffer);
    }
}