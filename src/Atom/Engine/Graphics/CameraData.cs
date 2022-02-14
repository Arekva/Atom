using Silk.NET.Vulkan;

namespace Atom.Engine;

public class CameraData
{
    private static SlimBuffer VPMatrices;
    
    private static Device Device { get; private set; }
    
    
    
    public uint Index { get; }
    
    

    public CameraData(uint index)
    {
        
    }


    public static void Initialize(Device? device = null)
    {
        Device used_device = Device = device ?? VK.Device;
        
        
    }

    public static void Cleanup()
    {
        VPMatrices.Destroy(Device);
    }
}