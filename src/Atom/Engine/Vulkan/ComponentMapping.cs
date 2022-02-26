using System.Runtime.CompilerServices;

namespace Atom.Engine;

public struct ComponentMapping
{
    public enum Swizzle
    {
        Identity    = Silk.NET.Vulkan.ComponentSwizzle.Identity,
        Zero        = Silk.NET.Vulkan.ComponentSwizzle.Zero,
        One         = Silk.NET.Vulkan.ComponentSwizzle.One,
        R           = Silk.NET.Vulkan.ComponentSwizzle.R,
        G           = Silk.NET.Vulkan.ComponentSwizzle.G,
        B           = Silk.NET.Vulkan.ComponentSwizzle.B,
        A           = Silk.NET.Vulkan.ComponentSwizzle.A,
    }
    
    
    
    public Swizzle R, G, B, A;

    

    public static ComponentMapping Zero { get; }
        = new (Swizzle.Zero, Swizzle.Zero,Swizzle.Zero, Swizzle.Zero);
    
    public static ComponentMapping One { get; } 
        = new (Swizzle.One, Swizzle.One,Swizzle.One, Swizzle.One);
    
    public static ComponentMapping Identity { get; }
        = new (Swizzle.Identity, Swizzle.Identity, Swizzle.Identity, Swizzle.Identity);
    
    public static ComponentMapping RGBA { get; }
        = new(Swizzle.R, Swizzle.G, Swizzle.B, Swizzle.A);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentMapping(Swizzle r, Swizzle g, Swizzle b, Swizzle a) => (R, G, B, A) = (r, g, b, a);
}