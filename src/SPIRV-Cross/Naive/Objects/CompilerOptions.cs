using SPIRVCross.Base;
using static SPIRVCross.Base.SPIRV;

namespace SPIRVCross.Naive
{
    internal class CompilerOptions : ISpvcObject<spvc_compiler_options>
    {
        public nint Handle { get; set; }
        
        public CompilerOptions() { }
        
        public void SetBool(CompilerOption option, bool value) => spvc_compiler_options_set_bool(this, (spvc_compiler_option)option, value).AsManaged().ThrowIfError("Unable to set compiler bool option.");
        public void SetUInt(CompilerOption option, u32 value) => spvc_compiler_options_set_uint(this, (spvc_compiler_option)option, value).AsManaged().ThrowIfError("Unable to set compiler u32 option.");

        public static implicit operator spvc_compiler_options(CompilerOptions opts) => opts.ToSpvc();
    }
}