using Silk.NET.Maths;

namespace Atom.Engine.Mesh;

public static class Wavefront
{
    public static (GVertex[], TIndex[], f64) Load<TIndex>(string path)
        where TIndex : unmanaged, IFormattable, IEquatable<TIndex>, IComparable<TIndex>
    {
        f64 bounding_sphere = 0.0D;
        
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
                    y: 1.0F - f32.Parse(values[2]))
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

                        bounding_sphere = Math.Max(bounding_sphere, Math.Max(Math.Max(Math.Abs(pos.X), Math.Abs(pos.Y)), Math.Abs(pos.Z)));

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
                
                // tangents
                u32 vertex_count_u32 = Scalar.As<TIndex, u32>(vertex_count);
                i32 vertex_count_i32 = (i32)vertex_count_u32;
                
                Vector4D<f32> work_tangent;
                
                Vector3D<f32> pos1 = vertices[vertex_count_i32+0].Position;
                Vector3D<f32> pos2 = vertices[vertex_count_i32+1].Position;
                Vector3D<f32> pos3 = vertices[vertex_count_i32+2].Position;
                
                Vector2D<f32> uv1 = vertices[vertex_count_i32+0].UV;
                Vector2D<f32> uv2 = vertices[vertex_count_i32+1].UV;
                Vector2D<f32> uv3 = vertices[vertex_count_i32+2].UV;
                
                Vector3D<f32> edge1    = pos2 - pos1;
                Vector3D<f32> edge2    = pos3 - pos1;
                Vector2D<f32> delta_uv1 = uv2 - uv1;
                Vector2D<f32> delta_uv2 = uv3 - uv1;
                
                f32 f = 1.0f / (delta_uv1.X * delta_uv2.Y - delta_uv2.X * delta_uv1.Y);
                work_tangent.X = f * (delta_uv2.Y * edge1.X - delta_uv1.Y * edge2.X);
                work_tangent.Y = f * (delta_uv2.Y * edge1.Y - delta_uv1.Y * edge2.Y);
                work_tangent.Z = f * (delta_uv2.Y * edge1.Z - delta_uv1.Y * edge2.Z);
                work_tangent.W = (f32)Math.CopySign(1.0, delta_uv2.Y - delta_uv1.Y);
                
                Vector3D<f32> tangent_xyz = Vector3D.Normalize<f32>(new(work_tangent.X, work_tangent.Y, work_tangent.Z));
                
                Vector4D<f32> tangent = new (tangent_xyz, -work_tangent.W);

                for (i32 i = 0; i < components.Length - 1; i++)
                {
                    GVertex previous = vertices[vertex_count_i32 + i];
                    vertices[vertex_count_i32 + i] = previous with { Tangent = tangent };
                }
            }
        }
        return (vertices.ToArray(), indices.ToArray(), bounding_sphere);
    }
}