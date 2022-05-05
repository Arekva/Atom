// ReSharper disable InconsistentNaming



using System.Runtime.CompilerServices;

using vki = Silk.NET.Vulkan.DebugUtilsMessageTypeFlagsEXT;
using ato = Atom.Engine.DebugUtilsMessageTypeFlags;



namespace Atom.Engine;



[Flags]
public enum DebugUtilsMessageTypeFlags : uint
{
    General     = vki.DebugUtilsMessageTypeGeneralBitExt    ,
    Validation  = vki.DebugUtilsMessageTypeValidationBitExt ,
    Performance = vki.DebugUtilsMessageTypePerformanceBitExt,
}

public static class DebugUtilsMessageTypeFlagsConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vki ToVk(this ato atom) => Unsafe.As<ato, vki>(ref atom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ato ToAtom(this vki vk) => Unsafe.As<vki, ato>(ref vk);
}