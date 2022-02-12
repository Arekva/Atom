using System;
using System.Runtime.InteropServices;
using SPIRVCross.Base;
using static SPIRVCross.Base.SPIRV;

namespace SPIRVCross.Naive
{
    public unsafe class Compiler : ISpvcObject<spvc_compiler>
    {
        public nint Handle { get; set; }
        public Context Context { get; set; }
        
        public Compiler() { }

        public void AddHeaderLine(string line)
        {
            u8* linePtr = LowLevel.GetPointer(line);
            spvc_compiler_add_header_line(this.ToSpvc(), linePtr).AsManaged().ThrowIfError("Unable to add header line: " + Context.GetLastErrorString());
            LowLevel.FreeString(linePtr);
        }
        public bool GetHlslCounterBuffer(u32 id, out u32 counterId)
        {
            u32 resultId;
            bool result = spvc_compiler_buffer_get_hlsl_counter_buffer(this, id, &resultId);
            counterId = resultId;
            return result;
        }
        public bool IsHlslCounterBuffer(u32 id) => spvc_compiler_buffer_is_hlsl_counter_buffer(this, id);
        public void BuildCombinedImageSamplers() => spvc_compiler_build_combined_image_samplers(this).AsManaged().ThrowIfError("Unable to build combined image samplers: " + Context.GetLastErrorString());
        public u32 BuildDummySamplerForCombinedImages()
        {
            u32 result;
            spvc_compiler_build_dummy_sampler_for_combined_images(this, &result).AsManaged().ThrowIfError("Unable to build dummy sampler for combined images: " + Context.GetLastErrorString());
            return result;
        }
        public string Compile()
        {
            u8* result = null;
            Result res = spvc_compiler_compile(this, &result).AsManaged();
            if (res != Result.Success)
            { 
                throw new Exception("Unable to compile the code: " + Context.GetLastErrorString());
            }

            return Marshal.PtrToStringAnsi((nint)result)!;
        }
        public CompilerOptions CreateCompilerOptions()
        {
            spvc_compiler_options opt;
            spvc_compiler_create_compiler_options(this, &opt);
            return SpvcObject.From<CompilerOptions, spvc_compiler_options>(opt);
        }
        public void InstallCompilerOptions(CompilerOptions options)
        {
            spvc_compiler_install_compiler_options(this, options).AsManaged().ThrowIfError("Unable to install options for the compiler: " + Context.GetLastErrorString());
        }
        public Resources CreateShaderResources()
        {
            spvc_resources resources;
            spvc_compiler_create_shader_resources(this, &resources).AsManaged().ThrowIfError("Unable to create the shader resources: " + Context.GetLastErrorString());
            return SpvcObject.From<Resources, spvc_resources>(resources);
        }
        public Resources CreateResourcesForActiveVariables(Set set)
        {
            spvc_resources resources;
            spvc_compiler_create_shader_resources_for_active_variables(this, &resources, set.ToSpvc()).AsManaged().ThrowIfError("Unable to create the shader active resources: " + Context.GetLastErrorString());
            return SpvcObject.From<Resources, spvc_resources>(resources);
        }
        public void FlattenBufferBlock(u32 id) => spvc_compiler_flatten_buffer_block(this, id).AsManaged().ThrowIfError("Unable to flatten block: " + Context.GetLastErrorString());
        public BufferRange[] GetActiveBufferRanges(u32 id)
        {
            spvc_buffer_range* rangesPtr;
            nuint count;
            spvc_compiler_get_active_buffer_ranges(this, id, (spvc_buffer_range*)&rangesPtr, &count).AsManaged().ThrowIfError("Unable to get active buffer ranges: " + Context.GetLastErrorString());
            Span<BufferRange> data = new((BufferRange*) rangesPtr, (i32) count);
            return data.ToArray();
        }
        public Set GetActiveInterfaceVariables()
        {
            spvc_set set;
            spvc_compiler_get_active_interface_variables(this, &set).AsManaged().ThrowIfError("Unable to get active interface variables: " + Context.GetLastErrorString());
            return SpvcObject.From<Set, spvc_set>(set);
        }
        public bool GetBinaryOffsetForDecoration(u32 id, Decoration decoration, out u32 wordOffset)
        {
            u32 offset;
            bool result = spvc_compiler_get_binary_offset_for_decoration(this, id, (SpvDecoration)decoration, &offset);
            wordOffset = offset;
            return result;
        }
        public Decoration[] GetBufferBlockDecorations(u32 id)
        {
            Decoration* decorationsPtr;
            nuint count;
            spvc_compiler_get_buffer_block_decorations(this, id, (SpvDecoration*)&decorationsPtr, &count).AsManaged().ThrowIfError("Unable to get buffer block decorations: " + Context.GetLastErrorString());
            Span<Decoration> decorations = new(decorationsPtr, (i32) count);
            return decorations.ToArray();
        }
        public string? GetCleansedEntryPointName(string name, ExecutionModel model)
        {
            u8* namePtr = LowLevel.GetPointer(name);
            u8* data = spvc_compiler_get_cleansed_entry_point_name(this, namePtr, (SpvExecutionModel)model);
            LowLevel.FreeString(namePtr);
            return LowLevel.GetString(data);
        }
        public CombinedImageSampler[] GetCombinedImageSamplers()
        {
            CombinedImageSampler* samplersPtr;
            nuint count;
            spvc_compiler_get_combined_image_samplers(this, (spvc_combined_image_sampler*)&samplersPtr, &count).AsManaged().ThrowIfError("Unable to get combined image samplers: " + Context.GetLastErrorString());
            Span<CombinedImageSampler> samplers = new(samplersPtr, (i32) count);
            return samplers.ToArray();
        }
        public Constant GetConstantHandle(u32 id) => SpvcObject.From<Constant, spvc_constant>(spvc_compiler_get_constant_handle(this, id).Handle);
        public u32 GetCurrentIdBound() => spvc_compiler_get_current_id_bound(this);
        public Capability[] GetDeclaredCapabilities()
        {
            Capability* capabilitiesPtr;
            nuint count;
            spvc_compiler_get_declared_capabilities(this, (SpvCapability*)&capabilitiesPtr, &count).AsManaged().ThrowIfError("Unable to get declared capabilities: " + Context.GetLastErrorString());
            Span<Capability> capabilities = new (capabilitiesPtr, (i32)count);
            return capabilities.ToArray();
        }
        public string?[] GetDeclaredExtensions()
        {
            u8* rawData;
            nuint count;
            spvc_compiler_get_declared_extensions(this, (u8*)&rawData, &count).AsManaged().ThrowIfError("Unable to get declared extensions: " + Context.GetLastErrorString());
            if (count != 0)
            {
                string?[] exts = new string[count];
                u32 currentLength = 0;
                for (u32 i = 0; i < count; i++)
                {
                    // each spvc string is terminated by null, which is handled
                    // by LowLevel.GetString . so everytime it's gonna shift the whole
                    // pointed value by the size of the previous string, so we start after
                    // the previous null end char and get the next string.
                    
                    u8* iterator = rawData + currentLength;
                    
                    string? current = LowLevel.GetString(iterator); // don't forget \/ the null ending char
                    if (current is not null) currentLength += (u32)current.Length + 1;
                    exts[i] = current;
                }
                
                return exts;
            }
            else
            {
                return Array.Empty<string?>();
            }
        }
        public u32 GetDeclaredStructMemberSize(Type type, u32 index)
        {
            nuint count;
            spvc_compiler_get_declared_struct_member_size(this, type, index, &count).AsManaged().ThrowIfError("Unable to get declared struct member size: " + Context.GetLastErrorString());
            return (u32) count;
        }
        public u32 GetDeclaredStructSize(Type type)
        {
            nuint size;
            spvc_compiler_get_declared_struct_size(this, type, &size).AsManaged().ThrowIfError("Unable to get declared struct size: " + Context.GetLastErrorString());
            return (u32) size;
        }
        public u32 GetDeclaredStructSizeRuntimeArray(Type type, nuint length)
        {
            nuint count;
            spvc_compiler_get_declared_struct_size_runtime_array(this, type, length, &count).AsManaged().ThrowIfError("Unable to get declared struct size runtime array: " + Context.GetLastErrorString());
            return (u32) count;
        }
        public u32 GetDecoration(u32 id, Decoration decoration) => spvc_compiler_get_decoration(this, id, (SpvDecoration)decoration);
        public string? GetDecorationString(u32 id, Decoration decoration) => LowLevel.GetString(spvc_compiler_get_decoration_string(this, id, (SpvDecoration) decoration));
        public EntryPoint[] GetEntryPoints()
        {
            nuint count;
            spvc_entry_point* entryPointsPtr;
            spvc_compiler_get_entry_points(this, &entryPointsPtr, &count).AsManaged().ThrowIfError("Unable to get entry points count: " + Context.GetLastErrorString());
            
            if (count == 0u)
            {
                return Array.Empty<EntryPoint>();
            }
            else
            {
                EntryPoint[] array = new EntryPoint[count];
                
                for (u32 i = 0; i < count; i++)
                {
                    array[i] = new EntryPoint
                    {
                        Name = LowLevel.GetString(entryPointsPtr[i].name),
                        ExecutionModel = (ExecutionModel)entryPointsPtr[i].execution_model
                    };
                }

                return array;
            }
        }
        public u32 GetExecutionModeArgument(ExecutionMode mode) => spvc_compiler_get_execution_mode_argument(this, (SpvExecutionMode)mode);
        public u32 GetExecutionModeArgumentByIndex(ExecutionMode mode, u32 index) => spvc_compiler_get_execution_mode_argument_by_index(this, (SpvExecutionMode)mode, index);
        public ExecutionModel GetExecutionModel() => (ExecutionModel)spvc_compiler_get_execution_model(this);
        public ExecutionMode[] GetExecutionModes()
        {
            ExecutionMode* modesPtr;
            nuint count;
            spvc_compiler_get_execution_modes(this, (SpvExecutionMode*)&modesPtr, &count).AsManaged().ThrowIfError("Unable to get execution modes: " + Context.GetLastErrorString());
            if (count == 0)
            {
                return Array.Empty<ExecutionMode>();
            }
            else
            {
                Span<ExecutionMode> modes = new(modesPtr, (i32) count);
                return modes.ToArray();
            }
        }
        public u32 GetMemberDecoration(u32 id, u32 memberIndex, Decoration decoration) => spvc_compiler_get_member_decoration(this, id, memberIndex, (SpvDecoration)decoration);
        public string? GetMemberDecorationString(u32 id, u32 memberIndex, Decoration decoration) => LowLevel.GetString(spvc_compiler_get_member_decoration_string(this, id, memberIndex, (SpvDecoration)decoration));
        public string? GetMemberName(u32 id, u32 memberIndex) => LowLevel.GetString(spvc_compiler_get_member_name(this, id, memberIndex));
        public string? GetName(u32 id) => LowLevel.GetString(spvc_compiler_get_name(this, id));
        public string? GetRemappedDeclaredBlockName(u32 id) => LowLevel.GetString(spvc_compiler_get_remapped_declared_block_name(this, id));
        public SpecializationConstant[] GetSpecializationConstants()
        {
            SpecializationConstant* constantsPtr;
            nuint count;
            spvc_compiler_get_specialization_constants(this, (spvc_specialization_constant*) &constantsPtr, &count).AsManaged().ThrowIfError("Unable to get specialization constants: " + Context.GetLastErrorString());
            Span<SpecializationConstant> constants = new(constantsPtr, (i32) count);
            return constants.ToArray();
        }
        public nint GetTypeHandle(u32 typeId) => spvc_compiler_get_type_handle(this, typeId).Handle;
        public u32 GetWorkGroupSizeSpecializationConstants(out SpecializationConstant x, out SpecializationConstant y, out SpecializationConstant z)
        {
            SpecializationConstant outX, outY, outZ;
            u32 result = spvc_compiler_get_work_group_size_specialization_constants(this, (spvc_specialization_constant*)&outX, (spvc_specialization_constant*)&outY,(spvc_specialization_constant*)&outZ);
            x = outX; y = outY; z = outZ;
            return result;
        }
        public bool HasDecoration(u32 id, Decoration decoration) => spvc_compiler_has_decoration(this, id, (SpvDecoration)decoration);
        public bool HasMemberDecoration(u32 id, u32 memberIndex, Decoration decoration) => spvc_compiler_has_member_decoration(this, id, memberIndex, (SpvDecoration)decoration); 

        // todo: set & hlsl stuff 
        
        //void t()
        //{
            //spvc_hlsl_resource_binding
            //spvc_compiler_hlsl_add_resource_binding()
            //SpvExecutionMode
            //spvc_compiler_get_execution_mode_argument()
        //}
        
        public static implicit operator spvc_compiler(Compiler compiler) => compiler.Handle;
    }
}