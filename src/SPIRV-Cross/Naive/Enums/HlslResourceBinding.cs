namespace SPIRVCross.Naive
{
    internal struct HlslResourceBinding
    {
        public ExecutionModel Stage;
        public u32 DescriptorSet;
        public u32 Binding;
        public HlslResourceBindingMapping Cbv;
        public HlslResourceBindingMapping Uav;
        public HlslResourceBindingMapping Srv;
        public HlslResourceBindingMapping Sampler;
    }
}