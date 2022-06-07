using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Silk.NET.Maths;

using Atom.Engine.Vulkan;



namespace Atom.Engine;



public partial class Camera
{
    // this structure is only used to properly represent the camera matrices gpu data onto the cpu
    [StructLayout(LayoutKind.Sequential)]
    private struct ViewProjection<T> where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        public Matrix4X4<T> View, Projection;
    }

    private static bool                           _isInitialized ;
        
    private static VulkanMemory                   _matricesMemory;
                  
    private static SlimBuffer                     _matricesBuffer;

    private static BufferSubresource              _matricesSubresource;

    private static MemoryMap<ViewProjection<f32>> _matricesCpuMap;

    private static ConcurrentBag<u32>             _availableIndices;

    static Camera()
    {
        _availableIndices = new ();
        for (u32 i = 0; i < MAX_CAMERA_COUNT; i++)
        {
            _availableIndices.Add(i);
        }
    }
    
    
    //private static CommandPool _renderPools
    
    
    
    private static vk.Device MatricesDevice => _matricesMemory.Device;

    public static BufferSubresource ShaderData => _matricesSubresource;

    public const string ShaderDataName = "_cameraMatrices";



    public static void Initialize(ReadOnlySpan<u32> queueFamilies, vk.Device? device = null)
    {
        vk.Device used_device = device ?? VK.Device;

        _matricesBuffer = new SlimBuffer(
            device     : used_device,
            size       : (u64)(MAX_CAMERA_COUNT * Unsafe.SizeOf<ViewProjection<f32>>() * Graphics.MAX_FRAMES_COUNT),
            usage      : BufferUsageFlags.StorageBuffer,
            sharingMode: vk.SharingMode.Exclusive, queueFamilies, 
            flags      : 0
        );

        _matricesBuffer.GetMemoryRequirements(used_device, out vk.MemoryRequirements reqs);

        const MemoryPropertyFlags SHARED_DEVICE_MEMORY = MemoryPropertyFlags.DeviceLocal  |
                                                         MemoryPropertyFlags.HostVisible  |
                                                         MemoryPropertyFlags.HostCoherent ;

        _matricesMemory = new VulkanMemory(
            device: used_device,
            size  : reqs.Size  ,
            memoryTypeIndex: VK.GPU.PhysicalDevice.FindMemoryType(
                typeFilter : reqs.MemoryTypeBits ,
                properties : SHARED_DEVICE_MEMORY)
        );

        _matricesBuffer.BindMemory(_matricesMemory.Whole);

        _matricesSubresource = new BufferSubresource(_matricesBuffer, _matricesMemory.Whole);

        _matricesCpuMap = _matricesMemory.Map<ViewProjection<f32>>();

        _isInitialized = true;
    }

    public static void Cleanup()
    {
        _isInitialized = false;
        
        _matricesCpuMap.Dispose(              );
        
        _matricesBuffer.Destroy(MatricesDevice);
        
        _matricesMemory.Dispose(              );
    }
    
    

    private void BorrowIndex()
    {
        if (!_isInitialized) throw new Exception("Camera engine data isn't initialized. Aborting borrowing.");
        
        if (_availableIndices.TryTake(out _index)) return;
        
        _index = UNINITIALIZED_INDEX;
        throw new Exception($"{MAX_CAMERA_COUNT} already have been created and the engine is at maximum capacity.");
    }

    private void RetrieveIndex()
    {
        if (_index == UNINITIALIZED_INDEX) return;
        
        _availableIndices.Add(_index);
        _index = UNINITIALIZED_INDEX;
    }

    private u64 BufferIndex(u32 frameIndex)
    {
        if (_index == UNINITIALIZED_INDEX)
        {
            throw new Exception("Camera hasn't been properly initialized, retrieving the buffer index is impossible in this state.");
        }
        else
        {
            return (u64)_index * Graphics.MAX_FRAMES_COUNT + frameIndex;
        }
    }

    private void TransferMatricesToGpu(u32 frameIndex, Camera? reference = null)
    {
        if (!_isInitialized) throw new Exception("Camera engine isn't initialized. Aborting gpu transfer.");
        
        ref ViewProjection<f32> gpu_location = ref _matricesCpuMap[BufferIndex(frameIndex)];
        gpu_location.View       = (Matrix4X4<f32>)ViewMatrix(reference);
        gpu_location.Projection = (Matrix4X4<f32>)ProjectionMatrix     ;
    }
}