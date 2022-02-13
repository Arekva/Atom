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

        Dictionary<DescriptorType, uint> descriptors_counts = new Dictionary<DescriptorType, uint>();

        int module_count = 0;
        foreach (IShaderModule shader_module in Modules)
        {
            IRasterModule raster_module = (IRasterModule) shader_module;
            
            descriptor_set_layouts[module_count] = raster_module.DescriptorLayout;

            foreach ((SPIRVCross.ResourceType type, Descriptor[] descriptors) in raster_module.Descriptors)
            {
                uint desc_count = (uint)descriptors.Length;

                if (desc_count == 0 || 
                    type 
                        is SPIRVCross.ResourceType.StageInput
                        or SPIRVCross.ResourceType.StageOutput
                        or SPIRVCross.ResourceType.PushConstant
                    ) continue;
                
                DescriptorType desc_type = ShaderModule.SpirvToVkDescMap[type];

                if (descriptors_counts.ContainsKey(desc_type))
                {
                    descriptors_counts[desc_type] += desc_count;
                }
                else
                {
                    descriptors_counts.Add(desc_type, desc_count);
                }
            }

            module_count++;
        }

        Span<DescriptorPoolSize> descriptor_sizes = stackalloc DescriptorPoolSize[16];

        int pool_size_count = 0;
        foreach ((DescriptorType type, uint count) in descriptors_counts)
        {
            descriptor_sizes[pool_size_count] = new DescriptorPoolSize(type, count);

            pool_size_count++;
        }
        
        PipelineLayout = new SlimPipelineLayout(
            device: Device, 
            setLayouts: descriptor_set_layouts[..module_count], 
            pushConstantRanges: ReadOnlySpan<PushConstantRange>.Empty);

        DescriptorPool = new SlimDescriptorPool(
            device: Device,
            maxSets: MaxMaterialsPerShader * Graphics.MaxFramesCount,
            poolSizes: descriptor_sizes[..pool_size_count]
        );
    }
}