// ReSharper disable InconsistentNaming
namespace Atom.Engine;

[Flags]
public enum MemoryPropertyFlags : uint
{
    DeviceLocal = vk.MemoryPropertyFlags.MemoryPropertyDeviceLocalBit,
    HostVisible = vk.MemoryPropertyFlags.MemoryPropertyHostVisibleBit,
    HostCoherent = vk.MemoryPropertyFlags.MemoryPropertyHostCoherentBit,
    HostCached = vk.MemoryPropertyFlags.MemoryPropertyHostCachedBit,
    LazilyAllocated = vk.MemoryPropertyFlags.MemoryPropertyLazilyAllocatedBit,
    DeviceCoherent_AMD = vk.MemoryPropertyFlags.MemoryPropertyDeviceCoherentBitAmd,
    DeviceUncached_AMD = vk.MemoryPropertyFlags.MemoryPropertyDeviceUncachedBitAmd,
    RDMA_Capable_NV = vk.MemoryPropertyFlags.MemoryPropertyRdmaCapableBitNV,
    Protected = vk.MemoryPropertyFlags.MemoryPropertyProtectedBit,
}

public static class MemoryPropertyFlagsConversion
{
    public static vk.MemoryPropertyFlags ToVk(this MemoryPropertyFlags memoryPropertyFlags) =>
        (vk.MemoryPropertyFlags)memoryPropertyFlags;
    public static MemoryPropertyFlags ToAtom(this vk.MemoryPropertyFlags vkMemoryPropertyFlags) =>
        (MemoryPropertyFlags)vkMemoryPropertyFlags;
}