namespace Atom.Engine;

public struct Clamp<T> where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
{
    public Clamp(T min, T max)
    {
        Min = min;
        Max = max;
    }

    public T Min;
    public T Max;
}