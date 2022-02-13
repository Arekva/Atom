namespace SPIRVCross.Naive
{
    public enum ExecutionModel
    {
        Vertex = 0,
        TessellationControl = 1,
        TessellationEvaluation = 2,
        Geometry = 3,
        Fragment = 4,
        Compute = 5,
        Kernel = 6,
        Task_NV = 5267,
        Mesh_NV = 5268,
        RayGeneration_KHR = 5313,
        Intersection_KHR = 5314,
        AnyHit_KHR = 5315,
        ClosestHit_KHR = 5316,
        Miss_KHR = 5317,
        Callable_KHR = 5318
    }
}