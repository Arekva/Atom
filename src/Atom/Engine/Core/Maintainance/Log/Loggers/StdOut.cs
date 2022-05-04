using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using StdIO = System.Console;

namespace Atom.Loggers;

public class StdOut
{
    private readonly TextWriter _defaultWriter;
    
    public StdOut()
    {
        SetupConsole();
        
        _defaultWriter = Console.Out;
        Stream stdout = Console.OpenStandardOutput();
        StreamWriter writer = new(stdout, Encoding.Default);
        writer.AutoFlush = true;
        Console.SetOut(writer);
    }
    
    public void Write(string ansi) => Console.Write(ansi);
    
    ~StdOut() => Console.SetOut(_defaultWriter);

    private static void SetupConsole()
    {
        // Don't care if not on Windows
        if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
        
        
        
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        //const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        
        

        nint iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
        if (GetConsoleMode(iStdOut, out uint outConsoleMode))
        {
            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING/* | DISABLE_NEWLINE_AUTO_RETURN*/;
            SetConsoleMode(iStdOut, outConsoleMode);
        }
    }
}
