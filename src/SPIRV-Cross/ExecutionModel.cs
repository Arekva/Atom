namespace SPIRVCross;

public enum ExecutionModel
{
    Vertex = 0, // 1
    TessellationControl = 1, // 2
    TessellationEvaluation = 2, // 4
    Geometry = 3, // 8
    Fragment = 4, // 16
    Compute = 5, // 32
    Kernel = 6, // ???
    Task = 5267, // 64  Nvidia
    Mesh = 5268, // 128 Nvidia
    RayGeneration = 5313, // 256 Khronos
    Intersection = 5314, // 4096 Khronos
    AnyHit = 5315, // 512        Khronos
    ClosestHit = 5316, // 1024   Khronos
    Miss = 5317, // 2048         Khronos
    Callable = 5318 // 8192      Khronos
}