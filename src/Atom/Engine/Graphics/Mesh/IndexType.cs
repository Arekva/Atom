using System.Runtime.CompilerServices;

namespace Atom.Engine;

public enum IndexType
{
    u8_EXT   = vk.IndexType.Uint8Ext,
    u16      = vk.IndexType.Uint16,
    u32      = vk.IndexType.Uint32,
    None_KHR = vk.IndexType.NoneKhr,
}

public static class IndexTypeConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vk.IndexType ToVk(this IndexType indexType) =>
        (vk.IndexType)indexType;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexType ToAtom(this vk.IndexType indexType) =>
        (IndexType)indexType;
}