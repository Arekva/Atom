using SPIRVCross;
using Type = SPIRVCross.Type;

namespace Atom.Engine.Shader;

public class Descriptor
{
    public string Name { get; set; }
    public uint Binding { get; set; }
    public uint Location { get; set; }
    
    public uint Offset { get; set; }
    
    public VectorDescriptor Vector { get; set; }
    public ArrayDescriptor Array { get; set; }
    
    public uint BitWidth { get; set; }
    
    public BaseType BaseType { get; set; }
    
    public StructDescriptor Struct { get; set; }
    
    //public ResourceType ResourceType { get; set; }


    public Descriptor(Type resource)
    {
        //ResourceType = type;
        
        uint dim_count = resource.GetArrayDimensionsCount();

        // get if the resource is an array
        uint array_elements = 1U;
        uint[] dims_lengths = System.Array.Empty<uint>();
        if (dim_count > 0)
        {
            dims_lengths = new uint[dim_count];

            for (uint i = 0u; i < dim_count; i++)
            {
                uint length = resource.GetArrayLength(i);
                dims_lengths[i] = length;

                array_elements *= length;
            }
        }
        // get the bit width of the resource
        uint bit_width = resource.GetSizeOfBit() * // base size x
                         Math.Max(1U, resource.GetMatrixColumns()) * // matrix columns x
                         Math.Max(1U, resource.GetVectorSize()) * // vector size x
                         array_elements; // number of elements in arrays
        
        
        Name = resource.GetName();
        Binding = resource.GetBinding();
        Location = resource.GetLocation();
        Offset = resource.GetOffset();
        Vector = new VectorDescriptor
        {
            VectorLength = resource.GetVectorSize(),
            MatrixColumns = resource.GetMatrixColumns()
        };
        Array = new ArrayDescriptor
        {
            IsArray = resource.IsArray(),
            DimensionsCount = dim_count,
            DimensionsLengths = dims_lengths
        };

        BitWidth = bit_width;
        BaseType = resource.GetBaseType();
        Struct = new StructDescriptor
        {
            Types = resource.GetStructComponents().ToArray()
        };
    }
}