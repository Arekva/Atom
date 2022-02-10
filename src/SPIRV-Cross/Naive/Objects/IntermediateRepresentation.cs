using SPIRVCross.Base;

namespace SPIRVCross.Naive
{
    internal class IntermediateRepresentation : ISpvcObject<spvc_parsed_ir>
    {
        public nint Handle { get; set; }
        
        public IntermediateRepresentation() { }
    }
}