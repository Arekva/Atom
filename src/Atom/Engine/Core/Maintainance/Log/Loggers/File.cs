using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using FIO = System.IO.File;

namespace Atom.Loggers;

public class File
{
    public string Path { get; set; } = "output.log";
    public int WriteWait { get; set; } = 1000;
    public Thread WriteThread { get; private set; }
    
    private CancellationTokenSource _writeWaitToken = new ();
    private ConcurrentQueue<string> _messages = new ();

    private volatile bool _disposed = false;
    public bool Disposed => _disposed;

    public File()
    {
        WriteThread = new Thread(LoopWrite)
        {
            Name = "File Logger",
            Priority = ThreadPriority.Lowest,
            IsBackground = false
            // do not close when engine does,
            // in case other logs needs to be read
        };
        WriteThread.Start();
    }

    private void OnEngineStop() => DumpToFile();

    private void DumpToFile() =>_writeWaitToken.Cancel();

    private void LoopWrite()
    {
        Log.OnEngineStop += OnEngineStop;

        FIO.Delete(Path); // reset log file

        while (!_disposed && !_writeWaitToken.IsCancellationRequested)
            try { Task.Delay(WriteWait).Wait(_writeWaitToken.Token); }
            catch { /* ignored */ } // if the file logger has to be dumped, stop waiting
            finally { WriteAllToFile(); }  // and write everything in file
    }
    
    private void WriteAllToFile()
    {
        if (_messages.Count == 0) return;
        
        StringBuilder str = new ();
        
        while (_messages.TryDequeue(out string? msg) && !string.IsNullOrEmpty(msg))
        {
            str.AppendLine(msg);
        }

        using StreamWriter file = FIO.AppendText(Path);
        file.Write(str);
    }
    
    public void EnqueueLog(string unformated) => _messages.Enqueue(unformated);
}
