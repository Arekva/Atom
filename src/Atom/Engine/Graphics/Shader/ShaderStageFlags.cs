namespace Atom.Engine;

[Flags]
public enum ShaderStageFlags
{
    Vertex = 1,
    TessellationControl = 2,
    TessellationEvaluation = 4,
    Geometry = 8,
    Fragment = 16,
    Compute = 32,
    AllGraphics = Fragment | Geometry | TessellationEvaluation | TessellationControl | Vertex,
    All = 2147483647,
    Raygen_KHR = 256,
    AnyHit_KHR = 512,
    ClosestHit_KHR = 1024,
    Miss_KHR = 2048,
    Intersection_KHR = 4096,
    Callable_KHR = 8192,
    Task_NV = 64,
    Mesh_NV = 128,
    SubpassShading_Huawei = 16384,
}