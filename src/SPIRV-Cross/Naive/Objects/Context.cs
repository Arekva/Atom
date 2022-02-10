using System;
using SPIRVCross;

using SPIRVCross.Base;
using static SPIRVCross.Base.SPIRV;

namespace SPIRVCross.Naive
{
    internal unsafe class Context : ISpvcObject<spvc_context>, IDisposable
    {
        public nint Handle { get; set; }

        /*  private Compiler? _compiler;
        // todo: support other backend languages
        public Compiler Compiler => _compiler ??= new Compiler(this, CaptureMode.TakeOwnership, Language.GLSL);
        */
        public Context()
        {
            Handle = CreateInternal().Handle;

            //SetErrorCallback(DefaultErrorCallback);
        }

        //private static void DefaultErrorCallback(void* userData, char* text) => Log.Error(LowLevel.GetString((u8*)text));

        public static Context Create() => SpvcObject.From<Context, spvc_context>(CreateInternal());
        public IntermediateRepresentation ParseSpirV(u8[] code)
        {
            SpvId* spirv; 
            fixed (u8* srcPtr = code) 
                spirv = (SpvId*) srcPtr;
            
            u32 wordCount = (u32)code.Length / sizeof(u32);
            
            spvc_parsed_ir ir; 
            spvc_context_parse_spirv(this, spirv, wordCount, &ir).AsManaged().ThrowIfError("Unable to parse SPIR-V source code: " + GetLastErrorString());
            return SpvcObject.From<IntermediateRepresentation, spvc_parsed_ir>(ir);
        }
        public Compiler CreateCompiler(Backend backend, IntermediateRepresentation ir, CaptureMode captureMode)
        {
            spvc_compiler compiler;
            spvc_context_create_compiler(this, (spvc_backend)backend, ir.ToSpvc(), (spvc_capture_mode)captureMode, &compiler);
            Compiler comp =  SpvcObject.From<Compiler, spvc_compiler>(compiler);
            comp.Context = this;
            return comp;
        }
        public void SetErrorCallback(ErrorCallback? callback)
        {
            spvc_error_callback cb = callback is null ? spvc_error_callback.Null : new (callback.Method.MethodHandle.Value);
            spvc_context_set_error_callback(this, cb, null);
        }
        public string? GetLastErrorString() => LowLevel.GetString(spvc_context_get_last_error_string(this));
        public void ReleaseAllocations() => spvc_context_release_allocations(this);
        public void Destroy() => spvc_context_destroy(this);
        
        
        private static spvc_context CreateInternal()
        {
            spvc_context context; 
            spvc_context_create(&context).AsManaged().ThrowIfError("Unable to create the SPVC context.");
            return context;
        }
        
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseAllocations();
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~Context() => Dispose();

        public static implicit operator spvc_context(Context context) => context.ToSpvc();
    }
}