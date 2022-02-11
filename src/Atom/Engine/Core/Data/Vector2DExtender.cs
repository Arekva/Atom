using System.Reflection;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class Vector2DExtender<T> where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
{
    private static MethodInfo _clampMethod;
    [ThreadStatic] private static object[] _argsArrayWidth;
    [ThreadStatic] private static object[] _argsArrayHeight;
    
    static Vector2DExtender()
    {
        Type type = typeof(T);
        Type[] args = { type, type, type };
        MethodInfo? clampMethod = typeof(Math).GetMethod("Clamp", args);
        _clampMethod = clampMethod ?? throw new NotSupportedException("Clamping not supported with type " + type.Name);
        
        _argsArrayHeight = new object[3];
        _argsArrayWidth = new object[3];
    }

    public static Vector2D<T> Clamp(Vector2D<T> value, Vector2D<T> min, Vector2D<T> max)
    {
        _argsArrayWidth![0] =  value.X;
        _argsArrayWidth![1] =    min.X;
        _argsArrayWidth![2] =    max.X;
            
        _argsArrayHeight![0] = value.Y;
        _argsArrayHeight![1] =   min.Y;
        _argsArrayHeight![2] =   max.Y;
            
        return new Vector2D<T>(
            x: (T)_clampMethod.Invoke(null, _argsArrayWidth )!,
            y: (T)_clampMethod.Invoke(null, _argsArrayHeight)!
        );
    }
}