using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SPIRVCross;

internal static unsafe class LowLevel
{
    // Inline, inline, IIINNNLINE !!!!!!
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeString(u8* @string) => FreePointer((nint)@string);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetString(u8* @string) => Marshal.PtrToStringAnsi((nint)@string);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static u8* GetPointer (string @string) => (u8*) Marshal.StringToHGlobalAnsi(@string);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreePointer(nint ptr) => Marshal.FreeHGlobal(ptr);
}