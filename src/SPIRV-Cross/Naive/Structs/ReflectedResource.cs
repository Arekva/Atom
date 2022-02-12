namespace SPIRVCross.Naive
{
    public struct ReflectedResource
    {
        public u32 Id { get; init; }
        public u32 BaseTypeId { get; init; }
        public u32 TypeId { get; init; }
        public string? Name { get; init; }
    }
}