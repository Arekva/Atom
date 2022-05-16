using mem = Atom.Engine.Vulkan.MemoryPropertyFlags;



namespace Atom.Engine;



public enum MemoryType : u32
{
    DeviceLocalShared = mem.HostVisible | mem.HostCoherent | mem.DeviceLocal
}