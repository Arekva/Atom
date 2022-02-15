using System.Runtime.CompilerServices;

namespace Atom.Engine;

public static class AMath
{
    /// <summary> Conversion ratio from degrees to radians. </summary>
    public const double DegToRad = Math.PI / 180D;
    /// <summary> Conversion ratio from radians to degrees. </summary>
    public const double RadToDeg = 180D / Math.PI;
    
    /// <summary> Conversion ratio from degrees to radians. </summary>
    public const float DegToRadF = (float)DegToRad;
    /// <summary> Conversion ratio from radians to degrees. </summary>
    public const float RadToDegF = (float)RadToDeg;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int To1D(int x, int y, int z, int width, int height) => x + width*y + width*height*z;
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int To1D(int x, int y, int width) => x + width*y;
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To2D(int index, int width, out int x, out int y)
    {
        x = index % width;
        y = index / width;
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To3D(int index, int width, int height, out int x, out int y, out int z)
    {
        x = index % width;
        y = (index / width)%height;
        z = index / (width*height);
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Map(double value, double oldLow, double oldHigh, double newLow, double newHigh) => newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Map(float value, float oldLow, float oldHigh, float newLow, float newHigh) => newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
}