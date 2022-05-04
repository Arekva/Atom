// ReSharper disable InconsistentNaming



using vkp = Silk.NET.Vulkan.PresentModeKHR;



namespace Atom.Engine;



public enum PresentMode : uint
{
    Immediate               = vkp.PresentModeImmediateKhr              ,
    Mailbox                 = vkp.PresentModeMailboxKhr                ,
    Fifo                    = vkp.PresentModeFifoKhr                   ,
    FifoRelaxed             = vkp.PresentModeFifoRelaxedKhr            ,
    SharedDemandRefresh     = vkp.PresentModeSharedDemandRefreshKhr    ,
    SharedContinuousRefresh = vkp.PresentModeSharedContinuousRefreshKhr,
}

public static class PresentModeConversion
{
    public static vk.PresentModeKHR ToVk(this PresentMode atom) =>
        (vk.PresentModeKHR)atom;
    public static PresentMode ToAtom(this vk.PresentModeKHR vk) =>
        (PresentMode)vk;
}