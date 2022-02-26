// ReSharper disable InconsistentNaming
namespace Atom.Engine.Vulkan;

[Flags]
public enum BufferUsageFlags : uint
{
    TransferSource = vk.BufferUsageFlags.BufferUsageTransferSrcBit,
    TransferDestination = vk.BufferUsageFlags.BufferUsageTransferDstBit,
    UniformTexelBuffer = vk.BufferUsageFlags.BufferUsageUniformTexelBufferBit,
    StorageTexelBuffer = vk.BufferUsageFlags.BufferUsageStorageTexelBufferBit,
    UniformBuffer = vk.BufferUsageFlags.BufferUsageUniformBufferBit,
    StorageBuffer = vk.BufferUsageFlags.BufferUsageStorageBufferBit,
    IndexBuffer = vk.BufferUsageFlags.BufferUsageIndexBufferBit,
    VertexBuffer = vk.BufferUsageFlags.BufferUsageVertexBufferBit,
    IndirectBuffer = vk.BufferUsageFlags.BufferUsageIndirectBufferBit,
    VideoDecodeSource_KHR = vk.BufferUsageFlags.BufferUsageVideoDecodeSrcBitKhr,
    VideoDecodeDestination_KHR = vk.BufferUsageFlags.BufferUsageVideoDecodeDstBitKhr,
    TransformFeedbackBuffer_EXT = vk.BufferUsageFlags.BufferUsageTransformFeedbackBufferBitExt,
    TransformFeedbackCounterBuffer_EXT = vk.BufferUsageFlags.BufferUsageTransformFeedbackCounterBufferBitExt,
    ConditionalRendering_EXT = vk.BufferUsageFlags.BufferUsageConditionalRenderingBitExt,
    AccelerationStructureBuildInputReadOnly_KHR = vk.BufferUsageFlags.BufferUsageAccelerationStructureBuildInputReadOnlyBitKhr,
    AccelerationStructureStorage_KHR = vk.BufferUsageFlags.BufferUsageAccelerationStructureStorageBitKhr,
    ShaderBindingTable_KHR = vk.BufferUsageFlags.BufferUsageShaderBindingTableBitKhr,
    Reserved18_QCOM = vk.BufferUsageFlags.BufferUsageReserved18BitQCom,
    VideoEncodeDestination_KHR = vk.BufferUsageFlags.BufferUsageVideoEncodeDstBitKhr,
    VideoEncodeSource_KHR = vk.BufferUsageFlags.BufferUsageVideoEncodeSrcBitKhr,
    Reserved21_AMD = vk.BufferUsageFlags.BufferUsageReserved21BitAmd,
    Reserved22_AMD = vk.BufferUsageFlags.BufferUsageReserved22BitAmd,
    ShaderDeviceAddress = vk.BufferUsageFlags.BufferUsageShaderDeviceAddressBit,
}

public static class BufferUsageFlagsConvertion
{
    public static vk.BufferUsageFlags ToVk(this BufferUsageFlags bufferUsageFlags) =>
        (vk.BufferUsageFlags)bufferUsageFlags;
    public static BufferUsageFlags ToAtom(this vk.BufferUsageFlags vkBufferUsageFlags) =>
        (BufferUsageFlags)vkBufferUsageFlags;
}