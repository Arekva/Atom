namespace Atom.Engine.Astro;

public interface ILoadModel
{
    public IEnumerator<u128> CellsToLoad(u32 depth);
}