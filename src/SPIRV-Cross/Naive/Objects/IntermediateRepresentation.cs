using SPIRVCross.Base;

namespace SPIRVCross.Naive
{
    public class IntermediateRepresentation : ISpvcObject<spvc_parsed_ir>
    {
        public nint Handle { get; set; }
        
        public IntermediateRepresentation() { }
    }
}