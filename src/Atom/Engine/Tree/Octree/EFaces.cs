using System;
using System.Collections.Generic;
using Silk.NET.Maths;

namespace Atom.Engine.Tree
{
    public static class EFaces
    {
        public static Vector3D<i32> GetNormal(this Faces faces)
        {
            Vector3D<i32> result = Vector3D<i32>.Zero;

            if (faces.HasFlag(Faces.Left))
                result += -Vector3D<i32>.UnitX;
            if (faces.HasFlag(Faces.Right))
                result += Vector3D<i32>.UnitX;
            if (faces.HasFlag(Faces.Down))
                result += -Vector3D<i32>.UnitY;
            if (faces.HasFlag(Faces.Up))
                result += Vector3D<i32>.UnitY;
            if (faces.HasFlag(Faces.Backward))
                result += -Vector3D<i32>.UnitZ;
            if (faces.HasFlag(Faces.Forward))
                result += Vector3D<i32>.UnitZ;
            
            return result;
        }
        
        public static Faces GetPositionToFacesNormalized(Vector3D<f64> pos)
        {
            Faces flags = Faces.None;

            if (pos.X < 0.0) flags |= Faces.Left;
            else flags |= Faces.Right;
            if (pos.Y < 0.0) flags |= Faces.Down;
            else flags |= Faces.Up;
            if (pos.Z < 0.0) flags |= Faces.Backward;
            else flags |= Faces.Forward;
            
            return flags;
        }
        
        public static char ToIndex(this Faces faces)
        {
            if (faces == Faces.None || faces == Faces.All) return 'X';
            if (faces.HasFlag(Faces.Left)) // left (A, C, E, G)
                if (faces.HasFlag(Faces.Down)) // left down (A, E)
                    if (faces.HasFlag(Faces.Backward)) // left down backward (A)
                        return 'A';
                    else return 'E'; // left down forward (E)
                else // left up (C, G)
                if (faces.HasFlag(Faces.Backward)) // left up backward (C)
                    return 'C';
                else return 'G'; // left up forward (G)
            else // right (B, D, F, H)
            if (faces.HasFlag(Faces.Down)) // right down (B, F)
                if (faces.HasFlag(Faces.Backward)) // right down backward (B)
                    return 'B';
                else return 'F'; // right down forward (F)
            else // right up (D, H)
            if (faces.HasFlag(Faces.Backward)) // right up backward (D)
                return 'D';
            else return 'H'; // right up forward (H)
        }

        public static Vector3D<f64> GetDirectionCubical(this Faces faces)
        {
            Vector3D<f64> dir = Vector3D<f64>.Zero;
            if (faces == Faces.None || faces == Faces.All) return dir;

            if (faces.HasFlag(Faces.Left)) dir.X = -1.0F;
            else dir.X = 1.0F;
            if (faces.HasFlag(Faces.Down)) dir.Y = -1.0F;
            else dir.Y = 1.0F;
            if (faces.HasFlag(Faces.Backward)) dir.Z = -1.0F;
            else dir.Z = 1.0F;

            return dir;
        }

        public static Vector3D<f64> GetDirectionSpherical(this Faces faces) => Vector3D.Normalize(GetDirectionCubical(faces));

        public static IEnumerable<Faces> GetFlags(this Faces input)
        {
            foreach (Faces value in Faces.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }
    }
}