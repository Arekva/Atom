using System.Runtime.CompilerServices;

namespace Atom.Engine.Vulkan;

public static class StructExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe Span<T> AsSpan<T>(ref this T @struct) where T : struct =>
        new (Unsafe.AsPointer(ref @struct), length: 1);
}