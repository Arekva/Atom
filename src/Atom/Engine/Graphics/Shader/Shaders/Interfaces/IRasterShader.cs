namespace Atom.Engine.Shader;

/// <summary> A raster-able shader. They are used to render triangles based meshes onto an image. </summary>
public interface IRasterShader : IShader 
{
    public IRasterShader? LightShader { get; }
    
    
    /// <summary> The vertex module of the shader. This is an obligatory module of the raster pipeline. </summary>
    ///<p> Vertex modules are able to control where the vertices will be placed on the screen. They are mostly used
    /// to apply a Model-View-Matrix so cameras can be simulated. </p>
    public IVertexModule VertexModule { get; }
    
    /// <summary> The tessellation control module of the shader. This is an optional module of the raster pipeline.
    /// </summary>
    /// <p> Tessellation control modules are able to submit which part of the mesh can have better details or should be
    /// reduced. </p>
    public ITessellationControlModule? TessellationControlModule { get; }
    
    /// <summary> The tessellation evaluation module of the shader. This is an optional module of the raster pipeline.
    /// </summary>
    /// <p> Tessellation evaluation modules are able to interpolate the vertex data to subdivide the mesh on-demand.
    /// </p>
    public ITessellationEvaluationModule? TessellationEvaluationModule { get; }
    
    /// <summary> The geometry module of the shader. This is an optional module of the raster pipeline. </summary>
    /// <p> Geometry modules are able to create vertices from the pipeline itself. </p>
    public IGeometryModule? GeometryModule { get; }
    
    /// <summary> The fragment module (or pixel module) of the shader. </summary>
    /// <p> Fragment modules are the modules which actually draw the pixels onto the image. For each pixel of the
    /// triangle display on the screen, the fragment shader has to output a color, like a solid color, a texture
    /// with a light effect and so on. </p>
    public IFragmentModule? FragmentModule { get; }
    
    /// <summary> The task module of the shader. This is an optional module of the raster pipeline, and is only
    /// available on NVIDIA GPUs. </summary>
    /// <p> Task modules can generate workgroups for the mesh module. </p>
    public ITaskModule? TaskModule { get; }
    
    /// <summary> The mesh module of the shader. This is an optional module of the raster pipeline, and is only
    /// available on NVIDIA GPUs. </summary>
    /// <p> Mesh modules can subdivide big models into smaller "meshlets" by working in workgroups, where each
    /// workgroup can submit triangles. </p>
    public IMeshModule? MeshModule { get; }
}