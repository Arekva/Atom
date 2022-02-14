using Atom.Engine.Global;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public class CameraData
{
    public const uint MaxCameraCount = 1024;
    
#region Handles
    
    private static DeviceMemory Memory;

    private static SlimBuffer VPMatrices;

    private static Device Device;
    
#endregion

    public uint Index { get; }

    public CameraData(uint index) => Index = index;


    public static unsafe void Initialize(Device? device = null)
    {
        Device used_device = Device = device ?? VK.Device;

        uint queue_family = 0U;
        VPMatrices = new SlimBuffer(
            device: used_device,
            size: (ulong)(MaxCameraCount * sizeof(CameraVP) * Graphics.MaxFramesCount),
            usage: BufferUsageFlags.UniformBuffer,
            sharingMode: SharingMode.Exclusive, queue_family.AsSpan(), 
            flags: 0
        );

        VPMatrices.GetMemoryRequirements(used_device, out MemoryRequirements reqs);

        Memory = new DeviceMemory(
            device: used_device,
            size: reqs.Size,
            memoryTypeIndex: VK.GPU.PhysicalDevice.FindMemoryType(reqs.MemoryTypeBits,
                properties: )
        );

    }

    public static void Cleanup()
    {
        VPMatrices.Destroy(Device);
    }
}