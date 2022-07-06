using System.Runtime.CompilerServices;

namespace Atom.Engine;

public class MemoryMap<T> : IDisposable, IMemoryMap where T : unmanaged
{
    public ref T this[u64 index]
    {
        get
        {
#if DEBUG
            if (index >= Segment.Length<T>())
            {
                throw new ArgumentOutOfRangeException(
                    paramName  : nameof(index), 
                    actualValue: index,
                    message    : $"A {Segment.Size} bytes {typeof(T)} memory contains {Segment.Length<T>()} elements, therefore index {index} is out of range."); 
            }
            
#endif

            unsafe
            {
                return ref Unsafe.AsRef<T>((T*)this + index);
            }
        }
    }

    public Span<T> this[Range range]
    {
        get
        {
            u64 segment_to_index = Segment.End;

            u64 start_val = (u64)range.Start.Value;
            u64 end_val   = (u64)range.End.Value  ;

            u64 start = range.Start.IsFromEnd ? segment_to_index - start_val : start_val;
            u64 end   = range.Start.IsFromEnd ? segment_to_index - end_val   : end_val  ;

#if DEBUG
            if (start >= end)
            {
                throw new ArgumentException("Range must be at least have 1 of length", nameof(range));
            }
#endif

            return AsSpan(start, unchecked(end - start));
        }
    }

    
    
    public isize         Handle  { get; }

    public MemorySegment Segment { get; }
    


    internal MemoryMap(isize handle, MemorySegment segment)
    {
        Handle  = handle ;
        Segment = segment;
    }

    public void Dispose()
    {
        Segment.Memory.Unmap(this);
        
        GC.SuppressFinalize(this);
    }

    ~MemoryMap() => Dispose();

    

    public Span<T> AsSpan() => AsSpan(length: Segment.Length<T>());

    public Span<T> AsSpan(u64 length) => AsSpan(start: 0UL, length);

    public Span<T> AsSpan(u64 start, u64 length)
    {
#if DEBUG
        if (length == 0UL)
        {
            throw new ArgumentOutOfRangeException(
                paramName  : nameof(length),
                actualValue: length,
                message    : "A memory span cannot represent 0 byte of data. (length is 0)");
        }
#endif

        u64 max_element_count = Segment.Length<T>();

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

        u64 stop = start + length;

#if DEBUG
        if (stop > max_element_count)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(length),
                actualValue: length,
                message:
                $"Span range ({stop} {typeof(T)}) exceeds the mapped memory range ({max_element_count} {typeof(T)})."
            );
        }
#endif

        u64 length_size = (unchecked(stop - start)) * (u64)Unsafe.SizeOf<T>();

#if DEBUG
        if (length_size > i32.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(length),
                message: $"The provided span parameters ([{start}..{length}]) total size ({length_size} bytes) " +
                         $"must be less or equal than the 32bit signed integer maximum value ({i32.MaxValue}). " +
                         $"(Thanks to the Common Language Specification / .NET to do everything in 32bit...)"
            );
        }
#endif
        unsafe
        {
            return new Span<T>((T*)Handle + start, Unsafe.As<u64, i32>(ref length_size));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once InconsistentNaming
    public MemoryMap<O> As<O>() where O : unmanaged => new(Handle, Segment);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(in MemoryMap<T> map) => map.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator void*(in MemoryMap<T> map) => (void*)map.Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator T*(in MemoryMap<T> map) => (T*)map.Handle;
}