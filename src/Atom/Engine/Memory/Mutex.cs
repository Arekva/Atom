namespace Atom.Engine;

public class Mutex<T>
{
    internal T Data;

    private readonly Mutex _readWriteMutex;
    
    public Mutex() => _readWriteMutex = new Mutex();

    public Mutex(T data) : this() => Data = data;
    
    public MutexLock<T> Lock()
    {
        _readWriteMutex.WaitOne();
        return new MutexLock<T>(this);
    } 
    
    internal void Release() => _readWriteMutex.ReleaseMutex();

    public static implicit operator Mutex<T>(T data) => new (data);
}