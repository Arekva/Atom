using SPIRVCross.Naive;

namespace SPIRVCross;

public class Type
{
    internal Naive.Type Naive { get; }
    internal Compiler Compiler;
    internal u32 Id { get; }
    
    internal Type(Compiler compiler, Naive.Type type, u32 id)
    {
        Compiler = compiler;
        Id = id;
        Naive = type;
    }

    public string GetName()
    {
        string? name = Compiler.GetName(Id);

        if (name is null)
        {
            throw new Exception("Type has no name.");
        }
        else
        {
            return name;
        }
    }
    public BaseType GetBaseType() => (BaseType) Naive.GetBaseType();

    public u32 GetSizeOf() => this.GetSizeOfBit() / 8;
    public u32 GetSizeOfBit() => Naive.GetBitWidth();

    public u32 GetArrayLength(u32 dimension) => Naive.GetArrayDimension(dimension);
    public u32 GetArrayDimensionsCount() => Naive.GetNumArrayDimensions();
    public bool IsArray() => GetArrayDimensionsCount() != 0;

    public u32 GetBinding() => Compiler.GetDecoration(Id, Decoration.Binding);
    public u32 GetLocation() => Compiler.GetDecoration(Id, Decoration.Location);
    public u32 GetOffset() => Compiler.GetDecoration(Id, Decoration.Offset);

    public u32 GetVectorSize() => Naive.GetVectorSize();
    public bool IsVector() => GetVectorSize() > 1U;

    public u32 GetMatrixColumns() => Naive.GetColumns();
    public bool IsMatrix() => GetMatrixColumns() > 1U;

    public IEnumerable<Type> GetStructComponents()
    {
        u32 members = Naive.GetNumMemberTypes();

        for (u32 i = 0; i < members; i++)
        {
            u32 id = Naive.GetMemberType(i); 
            Naive.Type type = new () { Handle = Compiler.GetTypeHandle(id) };
            yield return new Type(Compiler, type, id);
        }
    }

    public override string ToString() => $"{GetName()} ({GetBaseType()})";
}