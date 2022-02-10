namespace SPIRVCross.Naive
{
    internal enum StorageClass
    {
        UniformConstant = 0,
        Input = 1,
        Uniform = 2,
        Output = 3,
        Workgroup = 4,
        CrossWorkgroup = 5,
        Private = 6,
        Function = 7,
        Generic = 8,
        PushConstant = 9,
        AtomicCounter = 10,
        Image = 11,
        StorageBuffer = 12,
        KhronosCallableData = 5328,
        KhronosIncomingCallableData = 5329,
        KhronosRayPayload = 5338,
        KhronosHitAttribute = 5339,
        KhronosIncomingRayPayload = 5342,
        KhronosShaderRecordBuffer = 5343,
        PhysicalStorageBuffer = 5349,
        IntelCodeSection = 5605,
    }
}