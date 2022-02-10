using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class SilkExtender
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector3D<T> RotateAround<T>(this Vector3D<T> @this, Vector3D<T> pivot, Quaternion<T> rotation) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
        =>  rotation.Multiply(@this - pivot) + pivot;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Vector3D<T> Multiply<T>(this Quaternion<T> @this, Vector3D<T> point) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        T X =  Scalar.Multiply(@this.X,Scalar<T>.Two);
        T Y =  Scalar.Multiply(@this.Y,Scalar<T>.Two);
        T Z =  Scalar.Multiply(@this.Z,Scalar<T>.Two);
        T XX = Scalar.Multiply(@this.X,X);
        T YY = Scalar.Multiply(@this.Y,Y);
        T ZZ = Scalar.Multiply(@this.Z,Z);
        T XY = Scalar.Multiply(@this.X,Y);
        T XZ = Scalar.Multiply(@this.X,Z);
        T YZ = Scalar.Multiply(@this.Y,Z);
        T WX = Scalar.Multiply(@this.W,X);
        T WY = Scalar.Multiply(@this.W,Y);
        T WZ = Scalar.Multiply(@this.W,Z);


        Vector3D<T> result = new();
        result.X = // ((1.0D - (YY + ZZ)) * point.X) + ((XY - WZ) * point.Y) + ((XZ + WY) * point.Z)
            Scalar.Add(Scalar.Add(
                 Scalar.Multiply(Scalar.Subtract(Scalar<T>.One, Scalar.Add(YY, ZZ)), point.X), 
                Scalar.Multiply(Scalar.Subtract(XY, WZ), point.Y)), 
                Scalar.Multiply(Scalar.Add(XZ, WY), point.Z));
        
        result.Y = // ((XY + WZ) * point.X) + ((1.0D - (XX + ZZ)) * point.Y) + ((YZ - WX) * point.Z)
            Scalar.Add(Scalar.Add(
                 Scalar.Multiply(Scalar.Add(XY, WZ), point.X), 
                Scalar.Multiply(Scalar.Subtract(Scalar<T>.One, Scalar.Add(XX, ZZ)), point.Y)),
                Scalar.Multiply(Scalar.Subtract(YZ, WX), point.Z));
        
        result.Z = // ((XZ - WY) * point.X) + ((YZ + WX) * point.Y) + ((1.0D - (XX + YY)) * point.Z)
            Scalar.Add(Scalar.Add(
                 Scalar.Multiply(Scalar.Subtract(XZ, WY),point.X), 
                Scalar.Multiply(Scalar.Add(YZ, WX), point.Y)),
                Scalar.Multiply(Scalar.Subtract(Scalar<T>.One, Scalar.Add(XX, YY)), point.Z)
            );
        return result;
    }

    public static Quaternion<T> FromCross<T>(Vector3D<T> up, Vector3D<T> direction) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        T dot = Vector3D.Dot(up, direction);
        T angle = Scalar.Acos(dot);

        Vector3D<T> axis = new Vector3D<T>(
            Scalar.Subtract(Scalar.Multiply(up.Y,direction.Z),Scalar.Multiply(up.Z,direction.Y)), 
            Scalar.Subtract(Scalar.Multiply(up.Z,direction.X), Scalar.Multiply(up.X,direction.Z)),
            Scalar.Subtract(Scalar.Multiply(up.X,direction.Y), Scalar.Multiply(up.Y,direction.X))
            );
        
        return Quaternion<T>.Normalize(Quaternion<T>.CreateFromAxisAngle(axis, angle));
    }
    
    public static float ToFloat<T>(T scalar) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        //TODO: si la conversion échoue, c'est qu'il dois s'agir d'un type propre au moteur; faire de quoi les traités
        return Scalar.As<T, float>(scalar);
    }
}
