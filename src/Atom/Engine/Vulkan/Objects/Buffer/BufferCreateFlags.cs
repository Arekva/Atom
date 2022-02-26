// ReSharper disable InconsistentNaming
namespace Atom.Engine.Vulkan;

[Flags]
public enum BufferCreateFlags : uint
{
    SparseBinding = vk.BufferCreateFlags.BufferCreateSparseBindingBit,
    SparseResidency = vk.BufferCreateFlags.BufferCreateSparseResidencyBit,
    SparseAliased = vk.BufferCreateFlags.BufferCreateSparseAliasedBit,
    Reserved5_AMD = vk.BufferCreateFlags.BufferCreateReserved5BitAmd,
    Protected = vk.BufferCreateFlags.BufferCreateProtectedBit,
    DeviceAddressCaptureReplay = vk.BufferCreateFlags.BufferCreateDeviceAddressCaptureReplayBitKhr, // 0x00000010
}

public static class BufferCreateFlagsConversion
{
    public static vk.BufferCreateFlags ToVk(this BufferCreateFlags bufferCreateFlags) =>
        (vk.BufferCreateFlags)bufferCreateFlags;
    public static BufferCreateFlags ToAtom(this vk.BufferCreateFlags vkBufferCreateFlags) =>
        (BufferCreateFlags)vkBufferCreateFlags;
}