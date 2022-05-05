// ReSharper disable InconsistentNaming



using System.Runtime.CompilerServices;

using vki = Silk.NET.Vulkan.DebugUtilsMessageSeverityFlagsEXT;
using ato = Atom.Engine.DebugUtilsMessageSeverityFlags;



namespace Atom.Engine;



[Flags]
public enum DebugUtilsMessageSeverityFlags : uint
{
    Verbose = vki.DebugUtilsMessageSeverityVerboseBitExt,
    Info    = vki.DebugUtilsMessageSeverityInfoBitExt   ,
    Warning = vki.DebugUtilsMessageSeverityWarningBitExt,
    Error   = vki.DebugUtilsMessageSeverityErrorBitExt  ,
}

public static class DebugUtilsMessageSeverityFlagsConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vki ToVk(this ato atom) => Unsafe.As<ato, vki>(ref atom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ato ToAtom(this vki vk) => Unsafe.As<vki, ato>(ref vk);
}