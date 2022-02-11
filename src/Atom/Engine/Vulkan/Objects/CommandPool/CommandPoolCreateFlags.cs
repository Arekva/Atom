// ReSharper disable InconsistentNaming
namespace Atom.Engine;

[Flags]
public enum CommandPoolCreateFlags : uint
{
    Transient = vk.CommandPoolCreateFlags.CommandPoolCreateTransientBit,
    ResetCommandBuffer = vk.CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit,
    Protected = vk.CommandPoolCreateFlags.CommandPoolCreateProtectedBit,
}

public static class CommandPoolCreateFlagsConversion
{
    public static vk.CommandPoolCreateFlags ToVk(this CommandPoolCreateFlags commandPoolCreateFlags) =>
        (vk.CommandPoolCreateFlags)commandPoolCreateFlags;
    public static CommandPoolCreateFlags ToAtom(this vk.CommandPoolCreateFlags vkCommandPoolCreateFlags) =>
        (CommandPoolCreateFlags)vkCommandPoolCreateFlags;
}