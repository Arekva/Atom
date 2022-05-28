using Atom.Engine.GraphicsPipeline;
using Atom.Engine.Shader;

namespace Atom.Engine;

public class FullScreen : IDisposable
{
    private readonly RasterizedMaterial _material;

    private static ColorBlending _blending;
    
    private const vk.ColorComponentFlags ALL_FLAGS =
        vk.ColorComponentFlags.ColorComponentRBit | vk.ColorComponentFlags.ColorComponentGBit |
        vk.ColorComponentFlags.ColorComponentBBit | vk.ColorComponentFlags.ColorComponentABit;

    static FullScreen()
    {
        _blending = new ColorBlending(ColorBlending.Default);
        _blending.DoLogicOperator = false;
        _blending.Attachments = new vk.PipelineColorBlendAttachmentState[1]
        {
            new () { ColorWriteMask = ALL_FLAGS }
        };
    }
    
    public FullScreen(string shaderNamespace, string shaderName, vk.RenderPass renderPass, u32 subpass) : 
        this(Shader.Shader.Load<IRasterShader>(shaderNamespace, shaderName), renderPass, subpass) {}
    
    public FullScreen(IRasterShader shader, vk.RenderPass renderPass, u32 subpass)
    {
        _material = new RasterizedMaterial(shader, renderPass, subpass, createPipeline: false);
        _material.ColorBlending = _blending;
        _material.CreatePipeline();
    }

    public void SetInputImage(string name, ImageSubresource image, u32 frameIndex = 0, vk.ImageLayout layout = vk.ImageLayout.ShaderReadOnlyOptimal) 
        => _material.WriteInput(name, image, frameIndex, layout);
    
    public void SetSampledImage(string name, Texture texture, u32 frameIndex = 0) 
        => _material.WriteImage<IFragmentModule>(name, texture, frameIndex: frameIndex);

    public void CmdDraw(CommandRecorder.RenderPassRecorder renderPassRecorder, Silk.NET.Maths.Vector2D<u32> extent, u32 frameIndex)
    {
        _material.CmdBindMaterial(renderPassRecorder.CommandBuffer, extent, 0);
        renderPassRecorder.Draw(vertexCount: 3);
    }

    public void Delete()
    {
        _material.Delete();
    }

    public void Dispose() => Delete();

    ~FullScreen() => Dispose();
}