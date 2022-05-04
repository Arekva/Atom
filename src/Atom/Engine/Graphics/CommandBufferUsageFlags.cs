// ReSharper disable InconsistentNaming



using System.Runtime.CompilerServices;

using vki = Silk.NET.Vulkan.CommandBufferUsageFlags;



namespace Atom.Engine;



[Flags]
public enum CommandBufferUsageFlags : uint
{
    OneTimeSubmit       = vki.CommandBufferUsageOneTimeSubmitBit     ,
    RenderPassContinue  = vki.CommandBufferUsageRenderPassContinueBit,
    SimultaneousUse     = vki.CommandBufferUsageSimultaneousUseBit   ,
}

public static class CommandBufferUsageFlagsConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vk.CommandBufferUsageFlags ToVk(this CommandBufferUsageFlags atom) =>
        Unsafe.As<CommandBufferUsageFlags, vk.CommandBufferUsageFlags>(ref atom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CommandBufferUsageFlags ToAtom(this vk.CommandBufferUsageFlags vk) =>
        Unsafe.As<vk.CommandBufferUsageFlags, CommandBufferUsageFlags>(ref vk);
}