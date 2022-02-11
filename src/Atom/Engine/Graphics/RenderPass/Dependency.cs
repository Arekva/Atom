using Silk.NET.Vulkan;

namespace Atom.Engine.RenderPass;

public class Dependency
{
    public DependencyFlags Flags { get; set; }

    public DependencyInfo Source { get; set; }
    public DependencyInfo Destination { get; set; }

    public Dependency(DependencyInfo source, DependencyInfo destination)
    {
        Source = source;
        Destination = destination;
    }
}