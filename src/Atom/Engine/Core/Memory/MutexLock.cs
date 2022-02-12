namespace Atom.Engine;

public class MutexLock<T> : IDisposable
{
    private readonly Mutex<T> _mutex;
    
    public ref T Data => ref _mutex.Data;

    internal MutexLock(Mutex<T> mutex) => _mutex = mutex;

    public void Dispose()
    {
        _mutex.Release();
        GC.SuppressFinalize(this);
    }

    ~MutexLock() => Dispose();
}