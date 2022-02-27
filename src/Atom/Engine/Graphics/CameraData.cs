using System.Runtime.CompilerServices;
using Atom.Engine.Global;
using Silk.NET.Maths;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class CameraData : IDisposable
{
    public const uint MaxCameraCount = 1024;
    
#region Handles
    
    private static VulkanMemory Memory;

    public static SlimBuffer VPMatrices;

    private static vk.Device Device;
    
#endregion

    private static MemoryMap<CameraVP> MatricesMap;

    private static Action<uint>? OnFrameUpdateInternal;

    public static void UpdateFrame(uint frameIndex)
    {
        OnFrameUpdateInternal?.Invoke(frameIndex);
    }
    
    
    
    public uint Index { get; }

    public Action<uint> OnFrameUpdate;

    public CameraData(uint index)
    {
        Index = index;

        OnFrameUpdateInternal += FrameUpdate; // always assigned by owner camera
    }

    private void FrameUpdate(uint frameIndex) => OnFrameUpdate!(frameIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Update(in CameraVP viewProjectionMatrices, uint frameIndex) 
        => ((CameraVP*)MatricesMap)[Index * Graphics.MaxFramesCount + frameIndex] = viewProjectionMatrices;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Update(ReadOnlySpan<Matrix4X4<float>> viewProjectionMatrices, uint frameIndex)
    {
        fixed (Matrix4X4<float>* p_matrices = viewProjectionMatrices)
        {
            System.Buffer.MemoryCopy(
                source: p_matrices,
                destination: (CameraVP*)MatricesMap + (Index * Graphics.MaxFramesCount + frameIndex), 
                destinationSizeInBytes: Memory.Size,
                sourceBytesToCopy: (ulong)sizeof(Matrix4X4<float>) * 2);
        }
    }

    public static unsafe void Initialize(vk.Device? device = null)
    {
        vk.Device used_device = Device = device ?? VK.Device;

        uint queue_family = 0U;
        VPMatrices = new SlimBuffer(
            device: used_device,
            size: (ulong)(MaxCameraCount * sizeof(CameraVP) * Graphics.MaxFramesCount),
            usage: BufferUsageFlags.StorageBuffer,
            sharingMode: vk.SharingMode.Exclusive, queue_family.AsSpan(), 
            flags: 0
        );

        VPMatrices.GetMemoryRequirements(used_device, out vk.MemoryRequirements reqs);

        Memory = new VulkanMemory(
            device: used_device,
            size: reqs.Size,
            memoryTypeIndex: VK.GPU.PhysicalDevice.FindMemoryType(reqs.MemoryTypeBits,
                properties: MemoryPropertyFlags.DeviceLocal | MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent)
        );

        VPMatrices.BindMemory(Memory.Whole);

        MatricesMap = Memory.Map<CameraVP>();
    }

    public static void Cleanup()
    {
        MatricesMap.Dispose();
        
        VPMatrices.Destroy(Device);
        
        Memory.Dispose();
    }

    public void Dispose()
    {
        OnFrameUpdateInternal -= FrameUpdate;
        
        GC.SuppressFinalize(this);
    }

    ~CameraData() => Dispose();
}