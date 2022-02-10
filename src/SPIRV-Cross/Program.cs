using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SPIRVCross.Naive;

namespace SPIRVCross;

public class Program : IDisposable
{
    internal Context Context { get; private set; }
    internal Compiler Compiler { get; private set; }
    internal Resources Resources { get; private set; }
    
    public ExecutionModel ExecutionModel { get; private set; }
    
    public u8[] Source { get; private set; }
    
        
    public Program(string path, Language backend = Language.Glsl)
    {
        if (path == null) throw new ArgumentNullException(nameof(path), "The SPIR-V program file path must be provided.");
        u8[] source = File.ReadAllBytes(path);
        
        LoadProgramInternal(source, backend);
    }
    
    public Program(u8[] source, Language backend = Language.Glsl)
    {
        if (source == null) throw new ArgumentNullException(nameof(source), "The source code of the SPIR-V program must be provided.");

        LoadProgramInternal(source, backend);
    }

    private void LoadProgramInternal(u8[] source, Language backend)
    {
        Context = Context.Create();
        
        IntermediateRepresentation code;
        try
        {
            code = Context.ParseSpirV(source);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("File is not a valid SPIR-V program.", e);
        }

        Source = source;
        
        Compiler = Context.CreateCompiler((Backend)backend, code, CaptureMode.TakeOwnership);
        
        Resources = Compiler.CreateShaderResources();

        ExecutionModel = (ExecutionModel)Compiler.GetExecutionModel();
    }

    /// <summary> Get all the entry points of the program, either they are defined program's main execution model or not. </summary>
    /// <returns> An enumerable collection of all the entry points of the program. </returns>
    /// <exception cref="EntryPointException"> The program has none or unnamed entry points. </exception>
    public IEnumerable<EntryPoint> GetEntryPoints()
    {
        Naive.EntryPoint[] naiveEntries = Compiler.GetEntryPoints();
        if (naiveEntries.Length == 0)
        {
            throw new EntryPointException("Program has no entry point.");
        }

        for (u32 i = 0; i < naiveEntries.Length; i++)
        {
            string? name = naiveEntries[i].Name;
            ExecutionModel model = (ExecutionModel) naiveEntries[i].ExecutionModel;

            if (name is null)
            {
                throw new EntryPointException($"{model} entry point has is unnamed.");
            }

            yield return new EntryPoint(model, name);
        }
    }

    /// <summary> Get the entry point corresponding to the program's main execution model. </summary>
    /// <returns> The first main entry point found. </returns>
    /// <exception cref="EntryPointException"> No entry point is defined for this program's execution model. </exception>
    public EntryPoint GetEntryPoint() => GetEntryPoint(this.ExecutionModel) ?? throw new EntryPointException("No entry point corresponding to the program's main execution model is defined.");

    /// <summary> Get the entry point corresponding to an execution model. </summary>
    /// <param name="model"> The execution model's entry to search for. </param>
    /// <returns> The first entry point found corresponding to the model, null if none. </returns>
    public EntryPoint? GetEntryPoint(ExecutionModel model)
    {
        EntryPoint entry = GetEntryPoints().FirstOrDefault(e => e.ExecutionModel == model);
        // if entry name is null (default), then no entry found.
        return entry.Name is null ? null : entry;
    }
    
    public IEnumerable<Type> GetUniformsBuffers() => GetResources(ResourceType.UniformBuffer);
    public IEnumerable<Type> GetStorageBuffers() => GetResources(ResourceType.StorageBuffer);
    public IEnumerable<Type> GetStageInputs() => GetResources(ResourceType.StageInput);
    public IEnumerable<Type> GetStageOutputs() => GetResources(ResourceType.StageOutput);
    public IEnumerable<Type> GetSubpassInputs() => GetResources(ResourceType.SubpassInput);
    public IEnumerable<Type> GetSampledImages() => GetResources(ResourceType.SampledImage);
    public IEnumerable<Type> GetAtomicCounters() => GetResources(ResourceType.AtomicCounter);
    public IEnumerable<Type> GetPushConstants() => GetResources(ResourceType.PushConstant);
    public IEnumerable<Type> GetSeparateImages() => GetResources(ResourceType.SeparateImage);
    public IEnumerable<Type> GetSeparateSamplers() => GetResources(ResourceType.SeparateSamplers);
    public IEnumerable<Type> GetAccelerationStructures() => GetResources(ResourceType.AccelerationStructure);
    public IEnumerable<Type> GetRayQueries() => GetResources(ResourceType.RayQuery);

    public IEnumerable<Type> GetAllResources()
     =>     GetUniformsBuffers       ()
    .Concat(GetStorageBuffers        ())
    .Concat(GetStageInputs           ())
    .Concat(GetStageOutputs          ())
    .Concat(GetSubpassInputs         ())
    .Concat(GetSampledImages         ())
    .Concat(GetAtomicCounters        ())
    .Concat(GetPushConstants         ())
    .Concat(GetSeparateImages        ())
    .Concat(GetSeparateSamplers      ())
    .Concat(GetAccelerationStructures())
    .Concat(GetRayQueries            ());
    
    public IEnumerable<Type> GetResources(ResourceType type)
    {
        ReflectedResource[] stageInputs = Resources.GetResourceListForType((Naive.ResourceType)type);
        for (i32 i = 0; i < stageInputs.Length; i++)
        {
            ReflectedResource input = stageInputs[i];
            string? name = input.Name;
            if (name is null)
            {
                throw new ResourceException("One of the resource is unnamed.");
            }
            else
            {
                Naive.Type naiveType = new ()
                {
                    Handle = Compiler.GetTypeHandle(input.TypeId)
                };
                
                
                yield return new Type(Compiler, naiveType, input.Id);
            }
        }
    }

    public u8[] Compile() => Compiler.Compile();

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Context.Dispose();
        GC.SuppressFinalize(this);
    }

    ~Program() => Dispose();
}