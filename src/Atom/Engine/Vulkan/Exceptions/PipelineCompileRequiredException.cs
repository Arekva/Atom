using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable]
public sealed class PipelineCompileRequiredException : VulkanException
{
    public PipelineCompileRequiredException() { }
    public PipelineCompileRequiredException(string message) : base(message) { }
    public PipelineCompileRequiredException(string message, int code) : base(message, code) { }
    public PipelineCompileRequiredException(string message, Exception inner) : base(message, inner) { }
    private PipelineCompileRequiredException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}