using Silk.NET.Vulkan;

namespace Atom.Engine.Shader;

public class RasterShader : Shader, IRasterShader
{
#region Modules

    public override IShaderModule? this[Type type]
    {
        get
        {
            if (type == typeof(IVertexModule)) return VertexModule;
            if (type == typeof(ITessellationControlModule)) return TessellationControlModule;
            if (type == typeof(ITessellationEvaluationModule)) return TessellationEvaluationModule;
            if (type == typeof(IGeometryModule)) return GeometryModule;
            if (type == typeof(IFragmentModule)) return FragmentModule;
            if (type == typeof(ITaskModule)) return TaskModule;
            if (type == typeof(IMeshModule)) return MeshModule;

            throw new ArgumentException("The provided type must be an interface to a shader module (such as IVertexModule, IFragmentModule, ...)", nameof(type));
        }
    }

    public override IEnumerable<IShaderModule> Modules
    {
        get
        {
            yield return VertexModule;
            if (TessellationControlModule != null) yield return TessellationControlModule;
            if (TessellationEvaluationModule != null) yield return TessellationEvaluationModule;
            if (GeometryModule != null) yield return GeometryModule;
            yield return FragmentModule;
            if (TaskModule != null) yield return TaskModule;
            if (MeshModule != null) yield return MeshModule;
        }
    }
    
    public IVertexModule VertexModule { get; }
    
    public ITessellationControlModule? TessellationControlModule { get; }
    
    public ITessellationEvaluationModule? TessellationEvaluationModule { get; }
    
    public IGeometryModule? GeometryModule { get; }
    
    public IFragmentModule FragmentModule { get; }
    
    public ITaskModule? TaskModule { get; }
    
    public IMeshModule? MeshModule { get; }
    
#endregion

    public RasterShader(
        string @namespace, string name, string? description, Version version,
        /* obligatory modules */ 
        IVertexModule vertex, IFragmentModule fragment, 
        /* optional modules */
        IGeometryModule? geometry = null,
        ITessellationControlModule? tessControl = null, ITessellationEvaluationModule? tessEval = null, 
        ITaskModule? task = null, IMeshModule? mesh = null,
        Device? device = null
    ) : base(@namespace, name, description, version, device)
    {
        VertexModule = vertex ?? throw new ArgumentNullException(nameof(vertex));
        TessellationControlModule = tessControl;
        TessellationEvaluationModule = tessEval;
        GeometryModule = geometry;
        FragmentModule = fragment ?? throw new ArgumentNullException(nameof(fragment));
        TaskModule = task;
        MeshModule = mesh;
        
        Span<SlimDescriptorSetLayout> descriptor_set_layouts = stackalloc SlimDescriptorSetLayout[7];
        
        int module_count = 0;
        foreach (IShaderModule shader_module in Modules)
        {
            IRasterModule raster_module = (IRasterModule) shader_module;
            descriptor_set_layouts[module_count] = raster_module.DescriptorLayout; 
            module_count++;
        }
        
        PipelineLayout = new SlimPipelineLayout(
            device: Device, 
            setLayouts: descriptor_set_layouts[..module_count], 
            pushConstantRanges: ReadOnlySpan<PushConstantRange>.Empty);
    }
}