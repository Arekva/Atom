namespace SPIRVCross.Naive
{
    internal interface ISpvcObject<T> where T : unmanaged
    {
        public nint Handle { get; set; }
    }
}