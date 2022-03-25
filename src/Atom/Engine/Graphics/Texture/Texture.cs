namespace Atom.Engine;

public class Texture : IDisposable
{
    public readonly ImageSubresource Subresource;

    public readonly TextureSampler Sampler;

    public readonly bool IsImageOwned;

    public readonly bool IsSampledOwned;

    public Texture(ImageSubresource subresource, TextureSampler? sampler = null, vk.Device? device = null)
    {
        Subresource = subresource;
        Sampler = sampler ?? new TextureSampler();
    }
    
    public Texture(
        IImage image, bool ownImage = true, 
        TextureSampler? sampler = null, bool ownSampler = true,
        vk.Device? device = null)
    {
        Subresource = image.CreateSubresource();
        Sampler = sampler ?? new TextureSampler();
        IsImageOwned = ownImage;
        IsSampledOwned = ownSampler;
    }

    public void Delete()
    {
        if (IsImageOwned)
        {
            Subresource.      Dispose();
            Subresource.Image.Dispose();
        }

        if (IsSampledOwned)
        {
            Sampler.          Dispose();
        }
        
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Delete();

    ~Texture() => Dispose();
}