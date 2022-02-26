using System;

namespace Atom.Game;

public class ANoise
{
    public static void SwapValues(ref double a, ref double b) => SwapValues<double>(ref a, ref b);
    public static void SwapValues(ref int a, ref int b) => SwapValues<int>(ref a, ref b);
        
    /// <summary>
    /// Swaps two values.
    /// The values within the the two variables are swapped.
    /// </summary>
    /// <param name="a">A variable containing the first value.</param>
    /// <param name="b">A variable containing the second value.</param>
    public static void SwapValues<T>(ref T a, ref T b)
    {
        T obj = a;
        a = b;
        b = obj;
    }

    /// <summary>
    /// Modifies a floating-point value so that it can be stored in a
    /// int 32 bits variable.
    /// 
    /// In Libnoise, the noise-generating algorithms are all integer-based;
    /// they use variables of type int 32 bits.  Before calling a noise
    /// function, pass the x, y, and z coordinates to this function to
    /// ensure that these coordinates can be cast to a int 32 bits value.
    /// 
    /// Although you could do a straight cast from double to int 32 bits, the
    /// resulting value may differ between platforms.  By using this function,
    /// you ensure that the resulting value is identical between platforms.
    /// </summary>
    /// <param name="value">A floating-point number.</param>
    /// <returns>The modified floating-point number.</returns>
    public static double ToInt32Range(double value)
    {
        if (value >= 1073741824.0)
            return 2.0 * Math.IEEERemainder(value, 1073741824.0) - 1073741824.0;
        return value <= -1073741824.0 ? 2.0 * Math.IEEERemainder(value, 1073741824.0) + 1073741824.0 : value;
    }
        
    /// <summary>
    /// Unpack the given short (Int16) value to an array of 2 bytes in big endian format.
    /// If the length of the buffer is too small, it wil be resized.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="buffer">The output buffer.</param>
    public static byte[] UnpackBigInt16(short value, ref byte[] buffer)
    {
        if (buffer.Length < 2)
            Array.Resize<byte>(ref buffer, 2);
        buffer[0] = (byte) ((uint) value >> 8);
        buffer[1] = (byte) value;
        return buffer;
    }
    /// <summary>
    /// Unpack the given integer (Int32) value to an array of 4 bytes in big endian format.
    /// If the length of the buffer is too small, it wil be resized.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="buffer">The output buffer.</param>
    public static byte[] UnpackBigInt32(int value, ref byte[] buffer)
    {
        if (buffer.Length < 4)
            Array.Resize<byte>(ref buffer, 4);
        buffer[0] = (byte) (value >> 24);
        buffer[1] = (byte) (value >> 16);
        buffer[2] = (byte) (value >> 8);
        buffer[3] = (byte) value;
        return buffer;
    }
        
    /// <summary>
    /// Unpack the given short (Int16) to an array of 2 bytes in little endian format.
    /// If the length of the buffer is too small, it wil be resized.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="buffer">The output buffer.</param>
    public static byte[] UnpackLittleInt16(short value, ref byte[] buffer)
    {
        if (buffer.Length < 2)
            Array.Resize<byte>(ref buffer, 2);
        buffer[0] = (byte) ((uint) value & (uint) byte.MaxValue);
        buffer[1] = (byte) (((int) value & 65280) >> 8);
        return buffer;
    }
    /// <summary>
    /// Unpack the given integer (Int32) to an array of 4 bytes in little endian format.
    /// If the length of the buffer is too small, it wil be resized.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="buffer">The output buffer.</param>
    public static void UnpackLittleInt32(int value, Span<byte> buffer)
    {
        buffer[0] = (byte) (value & (int) byte.MaxValue);
        buffer[1] = (byte) ((value & 65280) >> 8);
        buffer[2] = (byte) ((value & 16711680) >> 16);
        buffer[3] = (byte) (((long) value & 4278190080L) >> 24);
    }

    /// <summary>(int)Math.floor(x) but faster.</summary>
    /// <param name="x">The value to floor.</param>
    public static int FastFloor(double x) => x < 0.0 ? (int) x - 1 : (int) x;
}