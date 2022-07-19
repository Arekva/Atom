namespace Atom.Engine;

public class Texture : IDisposable
{
    public readonly ImageSubresource Subresource;

    public readonly TextureSampler Sampler;

    public readonly bool IsImageOwned;

    public readonly bool IsSamplerOwned;

    public Texture(ImageSubresource subresource, TextureSampler? sampler = null, vk.Device? device = null)
    {
        Subresource = subresource;
        Sampler     = sampler!;
        if (sampler == null)
        {
            Sampler = new TextureSampler(device);
            IsSamplerOwned = true;
        }
        else
        {
            IsSamplerOwned = false;
        }

        IsImageOwned = false;
    }
    
    public Texture(string path) : this (image: Loaders.DDS.Load(stream: File.OpenRead(path))) { }
    
    public Texture(
        Image image, bool ownImage = true, 
        TextureSampler? sampler = null, bool ownSampler = true)
    {
        Subresource = image.CreateSubresource();
        Sampler = sampler ?? new TextureSampler();
        IsImageOwned = ownImage;
        IsSamplerOwned = ownSampler;
    }

    public void Delete()
    {
        if (IsImageOwned)
        {
            Subresource.      Dispose();
            Subresource.Image.Dispose();
        }

        if (IsSamplerOwned)
        {
            Sampler.          Dispose();
        }
        
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Delete();

    ~Texture() => Dispose();
}