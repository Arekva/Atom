using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Atom.Engine;

public unsafe class Pin<T> : IDisposable where T : unmanaged
{
    private T* _pinned;

    public T* Pointer => _pinned;

    public T[]? _baseArray;
    
    
    
    public ref T Data => ref Unsafe.AsRef<T>(_pinned);
    public T[] Array => _baseArray ?? throw new Exception("Pinned data is not an array.");
    
    public int Size { get; }

    
    
    private GCHandle _handle;

    private bool _disposed;
    
    public Pin(ref T data)
    {
        Size = 1;
        
        _handle = GCHandle.Alloc(data, GCHandleType.Pinned);

        _pinned = (T*)_handle.AddrOfPinnedObject();
    }
    
    public Pin(T[] array)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        
        Size = array.Length;
        _baseArray = array;

        _handle = GCHandle.Alloc(array, GCHandleType.Pinned);

        _pinned = (T*)_handle.AddrOfPinnedObject();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _handle.Free();
        GC.SuppressFinalize(this);
    }

    ~Pin() => Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Pin<T> pin) => pin._pinned;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pin<T>(in T data) => new(ref Unsafe.AsRef(in data));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pin<T>(T[] array) => new(array);
}