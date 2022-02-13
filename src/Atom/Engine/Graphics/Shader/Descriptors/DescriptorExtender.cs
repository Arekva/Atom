using System.Text;
using Silk.NET.Vulkan;
using SPIRVCross;
using Type = SPIRVCross.Type;

namespace Atom.Engine.Shader;

public static class DescriptorExtender
{
    public static string BeautifiedName(this Descriptor @this)
    {
        StringBuilder builder = new ();

        builder.Append($"{@this.Binding}@{@this.Location} ");
        builder.Append($"[{@this.GetType().Name}] ");
        builder.Append(@this.Name + " ");

        BaseType baseType = @this.BaseType;

        string BeautifyCsharp()
        {
            string csTypeName = baseType.ToString().ToLower();
            StringBuilder subBuilder = new();
            if (@this.Vector.IsVector)
            {
                if (@this.Vector.IsMatrix) subBuilder.Append($"Matrix{@this.Vector.MatrixColumns}X{@this.Vector.VectorLength}<{csTypeName}>");
                else subBuilder.Append($"Vector{@this.Vector.VectorLength}D<{csTypeName}>");
            }
            else subBuilder.Append(csTypeName);

            if (@this.Array.IsArray)
            {
                subBuilder.Append('[');
                uint dims = @this.Array.DimensionsCount;
                for (uint i = 0; i < dims; i++)
                {
                    subBuilder.Append(@this.Array.DimensionsLengths[i]);
                    if (i < dims - 1) subBuilder.Append(',');
                }
                subBuilder.Append(']');
            }

            return subBuilder.ToString();
        }
        
        builder.Append(baseType switch
        {
            BaseType.Unknown 
                or BaseType.Void 
                or BaseType.AtomicCounter 
                or BaseType.Image
                or BaseType.SampledImage
                or BaseType.Sampler
                or BaseType.AccelerationStructure 
                => baseType.ToString(),
            _ => BeautifyCsharp()
        });

        int structLength = @this.Struct.Types.Length;
        if (structLength > 0)
        {
            builder.Append("\n{\n");
            for (int i = 0; i < structLength; i++)
            {
                builder.Append('\t');
                builder.Append(@this.Struct.Types[i].BeautifiedName());
                builder.Append('\n');
            }
            builder.Append("}\n");
        }

        return builder.ToString();
    }
    
    public static string BeautifiedName(this Type @this)
    {
        StringBuilder builder = new ();

        builder.Append(@this.GetName() ?? "<unnamed>");

        BaseType baseType = @this.GetBaseType();

        string BeautifyCsharp()
        {
            string csTypeName = baseType.ToString().ToLower();
            StringBuilder subBuilder = new();
            if (@this.IsVector())
            {
                if (@this.IsMatrix()) subBuilder.Append($"Matrix{@this.GetMatrixColumns()}X{@this.GetVectorSize()}<{csTypeName}>");
                else subBuilder.Append($"Vector{@this.GetVectorSize()}D<{csTypeName}>");
            }
            else subBuilder.Append(csTypeName);

            if (@this.IsArray())
            {
                subBuilder.Append('[');
                uint dims = @this.GetArrayDimensionsCount();
                for (uint i = 0; i < dims; i++)
                {
                    subBuilder.Append(@this.GetArrayLength(dims-i-1));
                    if (i < dims - 1) subBuilder.Append(',');
                }
                subBuilder.Append(']');
            }

            return subBuilder.ToString();
        }
        
        builder.Append(baseType switch
        {
            BaseType.Unknown 
                or BaseType.Void 
                or BaseType.AtomicCounter 
                or BaseType.Image
                or BaseType.SampledImage
                or BaseType.Sampler
                or BaseType.AccelerationStructure 
                => baseType.ToString(),
            _ => BeautifyCsharp()
        });

        return builder.ToString();
    }


    private static char[] _vecComponents = { 'R', 'G', 'B', 'A' };
    private const uint Bit = sizeof(byte) * 8; 

    private static Dictionary<string, uint> _typeBitSize = new()
    {
        { "sbyte", sizeof(sbyte) * Bit },
        { "byte", sizeof(byte) * Bit },
        { "short", sizeof(short) * Bit },
        { "ushort", sizeof(ushort) * Bit },
        { "int", sizeof(int) * Bit },
        { "uint", sizeof(uint) * Bit },
        { "long", sizeof(long) * Bit },
        { "ulong", sizeof(ulong) * Bit },
        { "half", /*sizeof(Half)*/ 2 * Bit },
        { "float", sizeof(float) * Bit },
        { "double", sizeof(double) * Bit },
    };
    
    private static Dictionary<string, string> _typeNames = new()
    {
        { "sbyte" ,  "Sint"  },
        { "byte"  ,  "Uint"  },
        { "short" ,  "Sint"  },
        { "ushort",  "Uint"  },
        { "int"   ,  "Sint"  },
        { "uint"  ,  "Uint"  },
        { "long"  ,  "Sint"  },
        { "ulong" ,  "Uint"  },
        { "half"  , "Sfloat" },
        { "float" , "Sfloat" },
        { "double", "Sfloat" },
    };
    public static Format GetDefaultVkFormat(this Descriptor @this)
    {
        string csType = @this.BaseType.ToString().ToLower();

        StringBuilder format = new ();
        uint compCount = @this.Vector.VectorLength;
        uint bitSize = _typeBitSize[csType];
        for (uint i = 0U; i < compCount; i++)
        {
            format.Append(_vecComponents[i]);
            format.Append(bitSize);
        }
        format.Append(_typeNames[csType]);

        return Enum.Parse<Format>(format.ToString());
    }
}