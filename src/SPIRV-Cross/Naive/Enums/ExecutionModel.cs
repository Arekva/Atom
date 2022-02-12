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
        NvidiaTask = 5267,
        NvidiaMesh = 5268,
        KhronosRayGeneration = 5313,
        KhronosIntersection = 5314,
        KhronosAnyHit = 5315,
        KhronosClosestHit = 5316,
        KhronosMiss = 5317,
        KhronosCallable = 5318
    }
}