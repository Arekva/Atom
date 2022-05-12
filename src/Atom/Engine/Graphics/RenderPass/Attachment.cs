using System.Runtime.CompilerServices;

namespace Atom.Engine.RenderPass;

public class Attachment
{
    internal vk.AttachmentDescription Description;
    
    public vk.AttachmentDescriptionFlags Flags
    {
        get => Description.Flags;
        set => Description.Flags = value;
    }

    /// <summary> The color format used to store the pixels </summary>
    public ImageFormat ColorFormat
    {
        get => Description.Format.ToAtom();
        set => Description.Format = value.ToVk();
    }

    public vk.SampleCountFlags Multisampling
    {
        get => Description.Samples;
        set => Description.Samples = value;
    }

    public AttachmentOperator Operators
    {
        get => new(Description.LoadOp, Description.StoreOp);
        set
        {
            Description.LoadOp = value.Load;
            Description.StoreOp = value.Store;
        }
    }

    public AttachmentOperator StencilOperators
    {
        get => new(Description.StencilLoadOp, Description.StencilStoreOp);
        set
        {
            Description.StencilLoadOp = value.Load;
            Description.StencilStoreOp = value.Store;
        }
    }

    public LayoutTransition Layouts
    {
        get => new(Description.InitialLayout, Description.FinalLayout);
        set
        {
            Description.InitialLayout = value.Initial;
            Description.FinalLayout = value.Final;
        }
    }

    public Attachment(
        ImageFormat format,
        AttachmentOperator operators, 
        LayoutTransition layouts,
        vk.SampleCountFlags multisampling = vk.SampleCountFlags.SampleCount1Bit)
    {
        Description = default;
        
        ColorFormat = format;
        Operators = operators;
        Layouts = layouts;
        Multisampling = multisampling;
    }
}