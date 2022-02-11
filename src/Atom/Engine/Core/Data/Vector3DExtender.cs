using System.Reflection;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class Vector3DExtender<T> where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
{
    private static MethodInfo _clampMethod;
    [ThreadStatic] private static object[] _argsArrayWidth;
    [ThreadStatic] private static object[] _argsArrayHeight;
    [ThreadStatic] private static object[] _argsArrayDepth;

    static Vector3DExtender()
    {
        Type type = typeof(T);
        Type[] args = { type, type, type };
        MethodInfo? clampMethod = typeof(Math).GetMethod("Clamp", args);
        _clampMethod = clampMethod ?? throw new NotSupportedException("Clamping not supported with type " + type.Name);
        
        _argsArrayHeight = new object[3];
        _argsArrayWidth = new object[3];
        _argsArrayDepth = new object[3];
    }

    public static Vector3D<T> Clamp(Vector3D<T> value, Vector3D<T> min, Vector3D<T> max)
    {
        _argsArrayWidth![0] =   value.X;
        _argsArrayWidth![1] =     min.X;
        _argsArrayWidth![2] =     max.X;
            
        _argsArrayHeight![0] = value.Y;
        _argsArrayHeight![1] =   min.Y;
        _argsArrayHeight![2] =   max.Y;
            
        _argsArrayDepth![0] =  value.Z;
        _argsArrayDepth![1] =    min.Z;
        _argsArrayDepth![2] =    max.Z;
            
        return new Vector3D<T>(
            x: (T)_clampMethod.Invoke(null, _argsArrayWidth )!,
            y: (T)_clampMethod.Invoke(null, _argsArrayHeight)!,
            z: (T)_clampMethod.Invoke(null, _argsArrayDepth )!
        );
    }
}