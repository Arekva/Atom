// ReSharper disable InconsistentNaming
namespace Atom.Engine.Vulkan;

[Flags]
public enum CommandBufferLevel : uint
{
    Primary = vk.CommandBufferLevel.Primary,
    Secondary = vk.CommandBufferLevel.Secondary
}

public static class CommandBufferLevelConversion
{
    public static vk.CommandBufferLevel ToVk(this CommandBufferLevel commandBufferLevel) =>
        (vk.CommandBufferLevel)commandBufferLevel;
    public static CommandBufferLevel ToAtom(this vk.CommandBufferLevel vkCommandBufferLevel) =>
        (CommandBufferLevel)vkCommandBufferLevel;
}