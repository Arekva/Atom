using System.Runtime.CompilerServices;

namespace Atom.Engine;

public struct CommandBuffer
{
    public SlimCommandBuffer Handle; // Opaque type for GPU command buffer

#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying

#endregion

#region User defined

#endregion

}