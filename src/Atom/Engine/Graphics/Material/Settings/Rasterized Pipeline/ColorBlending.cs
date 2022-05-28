using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Atom.Engine.GraphicsPipeline;

public class ColorBlending : IRasterSettings, IDisposable
{
    public const ColorComponentFlags ALL_FLAGS =
        ColorComponentFlags.ColorComponentRBit | ColorComponentFlags.ColorComponentGBit |
        ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit;

    public static ColorBlending Default { get; } = new()
    {
        LogicOperator = LogicOp.Clear,
        Attachments = new[]
        {
            new PipelineColorBlendAttachmentState { ColorWriteMask = ALL_FLAGS },
            new PipelineColorBlendAttachmentState { ColorWriteMask = ALL_FLAGS },
            new PipelineColorBlendAttachmentState { ColorWriteMask = ALL_FLAGS }
        },
        BlendConstants = new Vector4D<float>(1.0F, 1.0F, 1.0F, 1.0F)
    };
    
    
    
    private PipelineColorBlendStateCreateInfo _blending;

    private Pin<PipelineColorBlendAttachmentState> _attachments;
    
    
    internal ref PipelineColorBlendStateCreateInfo State => ref _blending;




    public unsafe ColorBlending()
    {
        _attachments = Array.Empty<PipelineColorBlendAttachmentState>();
        _blending = new PipelineColorBlendStateCreateInfo(flags: 0);
    }
    
    public unsafe ColorBlending(ColorBlending cloneFrom)
    {
        // clone attachment states
        PipelineColorBlendAttachmentState* states = cloneFrom._attachments;

        int attach_count = cloneFrom._attachments.Size;
        PipelineColorBlendAttachmentState[] new_data = new PipelineColorBlendAttachmentState[attach_count];
        Array.Copy(cloneFrom._attachments._baseArray!, new_data, attach_count);
        
        _attachments = new_data;
        
        _blending = cloneFrom._blending;   
    }
    
    public IMaterialSettings Clone() => new ColorBlending(this);


    public void Dispose()
    {
        _attachments.Dispose();
        GC.SuppressFinalize(this);
    }

    ~ColorBlending() => Dispose();
    

    public bool DoLogicOperator
    {
        get => _blending.LogicOpEnable;
        set => _blending.LogicOpEnable = value;
    }

    public LogicOp LogicOperator
    {
        get => _blending.LogicOp;
        set => _blending.LogicOp = value;
    }

    public unsafe PipelineColorBlendAttachmentState[] Attachments
    {
        get => _attachments.Array;
        set
        {
            _attachments.Dispose();

            PipelineColorBlendAttachmentState[] data = value ?? Array.Empty<PipelineColorBlendAttachmentState>();

            _attachments = data;
            
            _blending.AttachmentCount = (uint)data.Length;
            _blending.PAttachments = _attachments;
        }
    }

    public unsafe Vector4D<float> BlendConstants
    {
        get => new(
            x: _blending.BlendConstants[0], 
            y: _blending.BlendConstants[1],
            z: _blending.BlendConstants[2], 
            w: _blending.BlendConstants[3]);
        set
        {
            _blending.BlendConstants[0] = value.X;
            _blending.BlendConstants[1] = value.Y;
            _blending.BlendConstants[2] = value.Z;
            _blending.BlendConstants[3] = value.W;
        }
    }
}