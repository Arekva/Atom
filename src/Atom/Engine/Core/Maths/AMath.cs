using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class AMath
{
    public const f64 TAU = 2.0D * Math.PI;
    
    /// <summary> Conversion ratio from degrees to radians. </summary>
    public const double DegToRad = Math.PI / 180D;
    /// <summary> Conversion ratio from radians to degrees. </summary>
    public const double RadToDeg = 180D / Math.PI;
    
    /// <summary> Conversion ratio from degrees to radians. </summary>
    public const float DegToRadF = (float)DegToRad;
    /// <summary> Conversion ratio from radians to degrees. </summary>
    public const float RadToDegF = (float)RadToDeg;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static i32 To1D(i32 x, i32 y, i32 z, i32 width, i32 height) => x + width*y + width*height*z;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static u32 To1D(u32 x, u32 y, u32 z, u32 width, u32 height) => x + width*y + width*height*z;
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static i32 To1D(i32 x, i32 y, i32 width) => x + width*y;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static u32 To1D(u32 x, u32 y, u32 width) => x + width*y;
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To2D(i32 index, i32 width, out i32 x, out i32 y)
    {
        x = index % width;
        y = index / width;
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To3D(i32 index, i32 width, i32 height, out i32 x, out i32 y, out i32 z)
    {
        x = index % width;
        y = (index / width)%height;
        z = index / (width*height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To3D(u32 index, u32 width, u32 height, out u32 x, out u32 y, out u32 z)
    {
        x = index % width;
        y = (index / width)%height;
        z = index / (width*height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To3D(i64 index, i64 width, i64 height, out i64 x, out i64 y, out i64 z)
    {
        x = index % width;
        y = (index / width)%height;
        z = index / (width*height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To3D(u64 index, u64 width, u64 height, out u64 x, out u64 y, out u64 z)
    {
        x = index % width;
        y = (index / width)%height;
        z = index / (width*height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void To3D(u64 index, u64 width, u64 height, out Vector3D<u64> indices)
    {
        indices.X = index % width;
        indices.Y = (index / width)%height;
        indices.Z = index / (width*height);
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Map(double value, double oldLow, double oldHigh, double newLow, double newHigh) => newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D<f64> Map(Vector3D<f64> value, Vector3D<f64> oldLow, Vector3D<f64> oldHigh, Vector3D<f64> newLow, Vector3D<f64> newHigh) => newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Map(float value, float oldLow, float oldHigh, float newLow, float newHigh) => newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static u64 Align(ulong number, ulong alignment)
    {
        if (alignment == 0) return number;
        u64 remainder = number % alignment;
        return remainder == 0 ? number : number + alignment - remainder;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static f64 AlignLower(f64 number, f64 alignment)
    {
        if (alignment == 0) return number;
        f64 remainder = number % alignment;
        return remainder == 0 ? number : number + alignment - remainder;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static f64 AlignUpper(f64 number, f64 alignment)
    {
        if (alignment == 0) return number;
        f64 remainder = number % alignment;
        return remainder == 0 ? number : number + alignment - remainder;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LinearInterpolation(double a, double b, double t) => (1.0D - t) * a + t * b;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double SCurve3(double a) => a*a*(3.0 - 2.0*a);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double SCurve5(double a) => a*a*a*(a*(a*6.0 - 15.0) + 10.0);
}