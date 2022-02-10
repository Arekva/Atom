using System;
using System.Runtime.CompilerServices;

namespace SPIRVCross.Naive
{
    internal static class SpvcObject
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static unsafe TSpvc ToSpvc<TSpvc>(this ISpvcObject<TSpvc> @object) where TSpvc : unmanaged
        {
            TSpvc spvc = new ();
            new Span<nint>(&spvc, 1)[0] = @object.Handle;
            return spvc;
        }
        
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)] public static unsafe TToCarbon From<TToCarbon, TFromSpvc>(TFromSpvc @object) where TToCarbon : ISpvcObject<TFromSpvc>, new() where TFromSpvc : unmanaged => new () { Handle = *(nint*)&@object };
    }
}