using System.Runtime.CompilerServices;

namespace Atom.Engine;

public class MemoryMap<T> : IDisposable, IMemoryMap where T : unmanaged
{
    public Span<T> this[Range range]
    {
        get
        {
            ulong segment_to_index = Segment.End;

            ulong start_val = (ulong)range.Start.Value;
            ulong end_val = (ulong)range.End.Value;
            
            ulong start = range.Start.IsFromEnd ? segment_to_index - start_val : start_val;
            ulong end = range.Start.IsFromEnd ? segment_to_index - end_val : end_val;

            if (start >= end)
            {
                throw new ArgumentOutOfRangeException(nameof(range), "Range must be at least have 1 of length");
            }

            return AsSpan(start, end - start);
        }
    }
    
    public nint Handle { get; }

    public MemorySegment Segment { get; }


    internal MemoryMap(nint handle, MemorySegment segment)
    {
        Handle = handle;
        Segment = segment;
    }
    
    public void Dispose()
    {
        Segment.Memory.Unmap(this);
        GC.SuppressFinalize(this);
    }

    ~MemoryMap() => Dispose();
    
    
    
    public Span<T> AsSpan() => AsSpan(length: Segment.Size / (ulong)Unsafe.SizeOf<T>());
    
    public Span<T> AsSpan(ulong length) => AsSpan(start: 0UL, length);

    public Span<T> AsSpan(ulong start, ulong length)
    {
#if DEBUG
        if (length == 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(length), 
                actualValue: length,
                message: "A memory span cannot represent 0 byte of data. (length is 0)");
        }
#endif

        ulong sizeof_element = (ulong)Unsafe.SizeOf<T>();
        ulong max_element_count = Segment.Size / sizeof_element;

#if DEBUG
        if (start >= max_element_count)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(start), 
                actualValue: start,
                message: $"Provided start index ({start}) is outside the " +
                         $"mapped memory range (length of {max_element_count})"
            );
        }
#endif

        ulong stop = start + length;

#if DEBUG
        if (stop > max_element_count)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(length),
                actualValue: length,
                message: $"Span range ({stop} {typeof(T)}) exceeds the mapped memory range ({max_element_count} {typeof(T)})."
            );
        }
#endif
        
        ulong length_size = (stop - start) * sizeof_element;
        
#if DEBUG
        if (length_size > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(length), 
                message: $"The provided span parameters ([{start}..{length}]) total size ({length_size} bytes) " +
                         $"must be less or equal than the 32bit signed integer maximum value ({int.MaxValue}). " +
                         $"(Thanks to the Common Language Specification / .NET to do everything in 32bit...)"
            );
        }
#endif
        unsafe
        {
            return new Span<T>((T*)Handle + start, Unsafe.As<ulong, int>(ref length_size));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(in MemoryMap<T> map) => map.AsSpan();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator void*(in MemoryMap<T> map) => (void*)map.Handle;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator T*(in MemoryMap<T> map) => (T*)map.Handle;
}