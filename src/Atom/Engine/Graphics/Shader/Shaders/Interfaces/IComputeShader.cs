namespace Atom.Engine.Shader;
    
/// <summary> A compute shader. They are used to perform heavily parallelized work, which can be graphic or not. </summary>
public interface IComputeShader : IShader
{
    /// <summary> The compute (and only) module of the compute pipeline. </summary>
    /// <p> Compute modules (shaders) are able to run arbitrary written programs into the compute cores of the GPU 
    /// (CUDA for NVIDIA). They are programs that can be heavily parallelized, such as image treatment or particle
    /// simulations, or even AIs. </p>
    public IComputeModule Module { get; }
}