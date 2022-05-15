using Silk.NET.Maths;

namespace Atom.Engine.Tree
{
    public static class EOctree
    {
        public static Faces[] CubeFaces = new Faces[22]
        {
            // corners
            Faces.Left | Faces.Down | Faces.Backward,
            Faces.Right | Faces.Down | Faces.Backward,
            Faces.Left | Faces.Up | Faces.Backward,
            Faces.Right | Faces.Up | Faces.Backward,
            Faces.Left | Faces.Down | Faces.Forward,
            Faces.Right | Faces.Down | Faces.Forward,
            Faces.Left | Faces.Up | Faces.Forward,
            Faces.Right | Faces.Up | Faces.Forward,
                
            // sides
            Faces.Left, Faces.Right, 
            Faces.Down, Faces.Up, 
            Faces.Backward, Faces.Forward,
                
            // vertices
            Faces.Left | Faces.Down,
            Faces.Right | Faces.Down,
            Faces.Backward | Faces.Down,
            Faces.Forward | Faces.Down,
            Faces.Left | Faces.Up,
            Faces.Right | Faces.Up,
            Faces.Backward | Faces.Up,
            Faces.Forward | Faces.Up,
        };
        public static Vector3D<f64> GetNodeCenter<T>(this Octree<T> node) where T : class
        {
            if(node.IsRoot) return Vector3D<f64>.Zero;

            return GetNodeCenter(node.Branch);
        }
        
        public static Vector3D<f64> GetNodeCenter(string index)
        {
            Vector3D<f64> finalPosition = Vector3D<f64>.Zero;
            for (int i = 0; i < index.Length; i++) 
                finalPosition += (GetIndexCenter(index[i]) / (1<<(int)(i + 1.0F)))*2.0F;

            return finalPosition;
        }
        
        public static float SubdivisionScale(uint subdivision) => 1.0F/(1<<(int)(subdivision + 1.0F))*2.0F;

        public static Vector3D<f64> GetIndexCenterScaled(char index, uint subdivision) => GetIndexCenter(index) * SubdivisionScale(subdivision);

        public static Faces GetFace<T>(this Octree<T> tree) where T : class
            => tree.Name[0] switch
            {
                'L' => Faces.Left, 'R' => Faces.Right,
                'D' => Faces.Down, 'U' => Faces.Up,
                'B' => Faces.Backward, 'F' => Faces.Forward
            };



        public static Vector3D<f64> GetNormalizedShift<T>(this Octree<T> tree) where T : class 
            => tree.GetFace() switch
            {
                Faces.Left      => -Vector3D<f64>.UnitX,
                Faces.Right     => Vector3D<f64>.UnitX,
                Faces.Down      => -Vector3D<f64>.UnitY,
                Faces.Up        => Vector3D<f64>.UnitY,
                Faces.Backward  => -Vector3D<f64>.UnitZ,
                Faces.Forward   => Vector3D<f64>.UnitZ,
            };
        
        
        public static Vector3D<f64> GetIndexCenter(char index) // normalized
            => index switch
            {
                'A' => new Vector3D<f64>(-0.5,-0.5,-0.5), // left down backward
                'B' => new Vector3D<f64>( 0.5,-0.5,-0.5), // right down backward
                'C' => new Vector3D<f64>(-0.5, 0.5,-0.5), // left up backward
                'D' => new Vector3D<f64>( 0.5, 0.5,-0.5), // right up backward
                'E' => new Vector3D<f64>(-0.5,-0.5, 0.5), // left down forward
                'F' => new Vector3D<f64>( 0.5,-0.5, 0.5), // right down forward
                'G' => new Vector3D<f64>(-0.5, 0.5, 0.5), // left up forward
                'H' => new Vector3D<f64>( 0.5, 0.5, 0.5), // right up forward
                _ => Vector3D<f64>.Zero // default as root: center of the octree cube
            };

        public static string ToGridSpace(Vector3D<f64> normalizedPosition, uint subdivision)
        {
            if(Math.Abs(normalizedPosition.X) > 1.0 || Math.Abs(normalizedPosition.Y) > 1.0 || Math.Abs(normalizedPosition.Z) > 1.0) 
                return null; // if outside of the grid

            if (subdivision == 0) return "X"; // just root.

            // the principle here will be to get the node coordinates (ABCDE...) of any
            // specified coordinates between -1.0 and 1.0 on both x y and z axis.
            // the subdivision parameter will determine the precision of the coordinates
            // i.e. sub 2 will give AB, while sub 6 could give ABDEAH.
            // the coordinates compute is achieved by getting the index relative
            // to the previous node, and then reuse that node in order to recalibrate the sampler:
            // i.e. the x coord is -0.48, so : (consider L as left and R as right)
            
            // 1st node: from -1.0 to 1.0, it is on the left side so L
            // 2nd node: from -1.0 to 0.0, remap so it gives a range from -1.0 to 1.0, which gives R => LR et ceterra. 
            
            //tl;dr take sample, zoom on sampled cube, sample, zoom on the new sampled cube...
            
            char[] coordinates = new char[subdivision];
            
            Vector3D<f64> zoomMin = Vector3D<f64>.One * -1.0;
            Vector3D<f64> zoomMax = Vector3D<f64>.One;

            Vector3D<f64> samplePos = normalizedPosition;
            Vector3D<f64> indexPos = Vector3D<f64>.Zero;

            for (int i = 0; i < subdivision; i++)
            {
                // zoom sample pos
                samplePos.X = AMath.Map(samplePos.X, zoomMin.X, zoomMax.X, -1.0, 1.0);
                samplePos.Y = AMath.Map(samplePos.Y, zoomMin.Y, zoomMax.Y, -1.0, 1.0);
                samplePos.Z = AMath.Map(samplePos.Z, zoomMin.Z, zoomMax.Z, -1.0, 1.0);

                Faces faces = EFaces.GetPositionToFacesNormalized(samplePos); // get face from sample pos
                char index = faces.ToIndex();
                coordinates[i] = index; // get index from position
                
                // get zoom bounds
                indexPos = EOctree.GetIndexCenter(index);
                zoomMin = indexPos - Vector3D<f64>.One/2.0;
                zoomMax = indexPos + Vector3D<f64>.One/2.0;
            }
            
            return new string(coordinates);
        }
    }
}