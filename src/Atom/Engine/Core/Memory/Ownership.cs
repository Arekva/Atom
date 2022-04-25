using System.Runtime.CompilerServices;

namespace Atom.Engine;

public class Ownership<T> : IDisposable
{
    public delegate void DoOwnedDelegate(ref T data);
    
    
    
    private bool _owned;
    private T    _data ;

    
    public bool Owned => _owned;
    public ref readonly T Data => ref _data;

    
    
    public Ownership(T data, bool owned)
    {
        _data  = data ;
        _owned = owned;
    }

    public Ownership<T> Move()
    {
        if (!_owned)
        {
            Log.Info("Data is not owned, borrowing instead of moving.");
            return Borrow();
        }
        
        _owned = false;
        return new Ownership<T>(Data, owned: true);
    }

    public Ownership<T> Borrow() => new (Data, owned: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Ownership<T>(T data) => new (data, owned: true);

    public void Do(DoOwnedDelegate action)
    {
        if (_owned) action(ref _data);
    }

    public void Dispose()
    {
        _owned = false;
        GC.SuppressFinalize(this);
    }

    ~Ownership() => Dispose();
}