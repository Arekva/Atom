using Atom.Engine.Astro;

namespace Atom.Engine.Generator;

public interface IGenerator
{
    public void Generate(u128 location, u32 depth, Span<Voxel> voxels);
}