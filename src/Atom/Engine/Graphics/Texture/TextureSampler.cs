using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class TextureSampler : IDisposable
{
    public readonly vk.Device Device;
    public readonly SlimSampler Sampler;

    public TextureSampler(vk.Device? device = null)
    {
        vk.Device used_device = device ?? VK.Device;
        Device  = used_device;
        Sampler = new SlimSampler(used_device);
    }

    public void Delete()
    {
        Sampler.Destroy(Device);
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Delete();

    ~TextureSampler() => Dispose();
}