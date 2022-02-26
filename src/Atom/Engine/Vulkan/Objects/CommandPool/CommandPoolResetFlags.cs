// ReSharper disable InconsistentNaming
namespace Atom.Engine.Vulkan;

[Flags]
public enum CommandPoolResetFlags : uint
{
    ReleaseResource = vk.CommandPoolResetFlags.CommandPoolResetReleaseResourcesBit,
    Reserved1 = vk.CommandPoolResetFlags.CommandPoolResetReserved1BitCoreavi
}

public static class CommandPoolResetFlagsConversion
{
    public static vk.CommandPoolResetFlags ToVk(this CommandPoolResetFlags commandPoolResetFlags) =>
        (vk.CommandPoolResetFlags)commandPoolResetFlags;
    public static CommandPoolResetFlags ToAtom(this vk.CommandPoolResetFlags vkCommandPoolResetFlags) =>
        (CommandPoolResetFlags)vkCommandPoolResetFlags;
}