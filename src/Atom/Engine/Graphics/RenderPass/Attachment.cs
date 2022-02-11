using Silk.NET.Vulkan;

namespace Atom.Engine.RenderPass;

public class Attachment
{
    internal AttachmentDescription Description;
    
    public AttachmentDescriptionFlags Flags
    {
        get => Description.Flags;
        set => Description.Flags = value;
    }

    /// <summary> The color format used to store the pixels </summary>
    public Format ColorFormat
    {
        get => Description.Format;
        set => Description.Format = value;
    }

    public SampleCountFlags Multisampling
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
        Format format,
        AttachmentOperator operators, 
        LayoutTransition layouts,
        SampleCountFlags multisampling = SampleCountFlags.SampleCount1Bit)
    {
        Description = default;
        
        ColorFormat = format;
        Operators = operators;
        Layouts = layouts;
        Multisampling = multisampling;
    }
}