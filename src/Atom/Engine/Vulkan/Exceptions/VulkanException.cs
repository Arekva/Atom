using System.Runtime.Serialization;
using Silk.NET.Vulkan;

namespace Atom.Engine;

/// <summary> Base exception type for all the exceptions related to Vulkan. </summary>
[Serializable]
public abstract class VulkanException : Exception
{
    public Result VkResult { get; }
    public VulkanException() {}
    public VulkanException(string message) : base(message) {}
    public VulkanException(string message, int code) : this(message) => VkResult = (Result)code;
    public VulkanException(string message, Exception inner) : base(message, inner) {}
    protected VulkanException(SerializationInfo info, StreamingContext context) : base(info, context) {}
}