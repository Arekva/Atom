using Silk.NET.Maths;

namespace Atom.Engine.Mesh;

public static class Wavefront
{
    public static (GVertex[], TIndex[]) Load<TIndex>(string path)
        where TIndex : unmanaged, IFormattable, IEquatable<TIndex>, IComparable<TIndex>
    {
        const i32 BASE_CAPACITY = (1 << 19) - 1; // 524 287

        List<Vector3D<f32>> v  = new(capacity: BASE_CAPACITY);
        List<Vector2D<f32>> vt = new(capacity: BASE_CAPACITY); // only using UV0
        List<Vector3D<f32>> vn = new(capacity: BASE_CAPACITY);

        List<GVertex> vertices = new (capacity: BASE_CAPACITY);
        List<TIndex > indices  = new (capacity: BASE_CAPACITY);
        TIndex indices_count = Scalar<TIndex>.Zero;

        foreach (string file_line in File.ReadLines(path))
        {
            string line = file_line.Trim();
            
            if (line.StartsWith("v "))
            { // standard vertex, just record it.
                string[] values = line.Split(' ', count: 4);
                v.Add(new Vector3D<f32>(
                    x: f32.Parse(values[1]), 
                    y: f32.Parse(values[2]),
                    z: f32.Parse(values[3]))
                ); 
            }
            else if (line.StartsWith("vt "))
            {
                string[] values = line.Split(' ', count: 3);
                vt.Add(new Vector2D<f32>(
                    x: f32.Parse(values[1]), 
                    y: f32.Parse(values[2]))
                ); 
            }
            else if (line.StartsWith("vt1 "))
            {
                throw new Exception("Meshes with UV1 aren't supported.");
            }
            else if (line.StartsWith("vt2 "))
            {
                throw new Exception("Meshes with UV2 aren't supported.");
            }
            else if (line.StartsWith("vn "))
            {
                string[] values = line.Split(' ', count: 4);
                vn.Add(new Vector3D<f32>(
                    x: f32.Parse(values[1]), 
                    y: f32.Parse(values[2]),
                    z: f32.Parse(values[3]))
                ); 
            }
            else if (line.StartsWith("f ")) // face
            {
                string[] components = line.Split(' ', count: 5);
                TIndex vertex_count = Scalar.As<i32, TIndex>(vertices.Count);
                if (components.Length == 4) // triangle face
                {
                    for (i32 i = 0; i < 3; i++)
                    {
                        ref readonly string comp = ref components[i + 1];
                        string[] comp_indices = comp.Split('/', 3);

                        Vector3D<f32> pos     = v [i32.Parse(comp_indices[0]) - 1];
                        Vector2D<f32> uv      = vt[i32.Parse(comp_indices[1]) - 1];
                        Vector3D<f32> normal  = vn[i32.Parse(comp_indices[2]) - 1];

                        vertices.Add(new GVertex
                        {
                            Position = pos,
                            UV = uv,
                            Normal = normal,
                            Tangent = Vector4D<Single>.Zero
                        });
                        
                        indices.Add(Scalar.Add(vertex_count, Scalar.As<i32, TIndex>(i)));
                    }
                }
                else // quad face
                {
                    for (i32 i = 0; i < 4; i++)
                    {
                        ref readonly string comp = ref components[i + 1];
                        string[] comp_indices = comp.Split('/', 3);

                        Vector3D<f32> pos     = v [i32.Parse(comp_indices[0]) - 1];
                        Vector2D<f32> uv      = vt[i32.Parse(comp_indices[1]) - 1];
                        Vector3D<f32> normal  = vn[i32.Parse(comp_indices[2]) - 1];
                        vertices.Add(new GVertex
                        {
                            Position = pos,
                            UV = uv,
                            Normal = normal,
                            Tangent = Vector4D<Single>.Zero
                        });
                    }
                    
                    // build triangles from quad
                    indices.Add(vertex_count);
                    indices.Add(Scalar.Add(vertex_count, Scalar<TIndex>.One));
                    indices.Add(Scalar.Add(vertex_count, Scalar<TIndex>.Two));
                    
                    indices.Add(vertex_count);
                    indices.Add(Scalar.Add(vertex_count, Scalar<TIndex>.Two));
                    indices.Add(Scalar.Add(vertex_count, Scalar.Add(Scalar<TIndex>.One, Scalar<TIndex>.Two)));
                }

            }
        }

        return (vertices.ToArray(), indices.ToArray());
    }
}