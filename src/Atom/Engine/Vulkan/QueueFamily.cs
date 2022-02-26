using Atom.Engine;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct QueueFamily
{
    private PhysicalDevice _physicalDevice;
    
    

    private QueueFamilyProperties _properties;

    private uint _index;



    internal PhysicalDevice PhysicalDevice => _physicalDevice;



    public uint Index => _index;

    public QueueFlags Flags => (QueueFlags)_properties.QueueFlags;

    public uint Count => _properties.QueueCount;

    public uint TimestampValidBits => _properties.TimestampValidBits;

    public Vector3D<uint> MinImageTransferGranularity
    {
        get
        {
            // Unsafe.As won't work with generic types, so let's do it old school style
            Extent3D gran_ref = _properties.MinImageTransferGranularity;
            unsafe
            {
                return *(Vector3D<uint>*)&gran_ref;
            }
        }
    }



    public QueueFamily(PhysicalDevice physicalDevice, QueueFamilyProperties properties, uint index)
    {
        _physicalDevice = physicalDevice;

        _properties = properties;

        _index = index;
    }
}