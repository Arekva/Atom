using Silk.NET.Maths;

namespace Atom.Engine;

public struct ClampVector2D<T> where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
{
    public ClampVector2D(Vector2D<T> min, Vector2D<T> max)
    {
        Min = min;
        Max = max;
    }

    public Vector2D<T> Min;
    public Vector2D<T> Max;
}