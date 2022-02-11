using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;

namespace Atom.Engine
{
    public static unsafe class LowLevel
    {
        // Inline, inline, IIINNNLINE !!!!!!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(nint ptr) => Marshal.FreeHGlobal(ptr);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(byte* @string) => Free((nint)@string);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(byte** strings) => SilkMarshal.Free((nint)strings);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? GetString(byte* @string) => Marshal.PtrToStringAnsi((nint)@string);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] GetStrings(byte** strings, int count) => SilkMarshal.PtrToStringArray((nint)strings, count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* GetPointer (string @string) => (byte*) Marshal.StringToHGlobalAnsi(@string);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte** GetPointer (string[] strings) => (byte**) SilkMarshal.StringArrayToPtr(strings);
    }
}