using SPIRVCross.Base;

namespace SPIRVCross.Naive
{
    public class Constant : ISpvcObject<spvc_constant>
    {
        public nint Handle { get; set; }
        
        public Constant() { }
    }
}