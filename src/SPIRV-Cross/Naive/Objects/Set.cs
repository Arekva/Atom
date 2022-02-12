using SPIRVCross.Base;

namespace SPIRVCross.Naive
{
    public class Set : ISpvcObject<spvc_set>
    {
        public nint Handle { get; set; }
        
        public Set() { }
    }
}