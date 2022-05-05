using System.Diagnostics;
using System.Text;

using Atom.Engine.ANSI;

namespace Atom;

public static class Log
{
    private static Atom.Loggers.File _fileLogger = new();
    private static Atom.Loggers.StdOut _stdLogger = new();

    private struct LevelInfo
    {
        public string DisplayName;
    }

    private static readonly LevelInfo TraceInfo =   new() { DisplayName = "|#9270CF,Trace|"   };
    private static readonly LevelInfo InfoInfo =    new() { DisplayName = "|#A1FCFF,Info |"   };
    private static readonly LevelInfo WarningInfo = new() { DisplayName = "|#FFFF00,Warn |"   };
    private static readonly LevelInfo ErrorInfo =   new() { DisplayName = "|#FF0000,Error|"   };
    private static readonly LevelInfo FatalInfo =   new() { DisplayName = "|,#FF0000Fatal|"   };
    
    public const string THREAD_SEPARER_COLOR = "#404040";

    private const string BANNER_COLOR = Richtext.ColorCode.FAINT;
    

    public static event Action? OnEngineStop;

    public static void TriggerEngineStop()
    {
        OnEngineStop?.Invoke();
    }

    private static string FormattedNow()
    {
        const string MAIN_DATE_COLOR = "#5BA83D";
        const string ALT_DATE_COLOR = "#5C5422";
        
        DateTime now = DateTime.Now;
        return $"|{BANNER_COLOR},[||{MAIN_DATE_COLOR},{now.Hour:D2}||{ALT_DATE_COLOR},:||{MAIN_DATE_COLOR},{now.Minute:D2}||{ALT_DATE_COLOR},:||{MAIN_DATE_COLOR},{now.Second:D2}||{ALT_DATE_COLOR},.||{MAIN_DATE_COLOR},{now.Millisecond:D3}||{BANNER_COLOR},]| ";
    }

    private static string FormattedThreadInfo(LevelInfo info) => $"{Thread.CurrentThread.Name ?? "CLR"}|{THREAD_SEPARER_COLOR},/|{info.DisplayName}";

    private static void InternalLog(LevelInfo levelInfo, object? obj)
    {
        StringBuilder builder = new();
        builder.Append(FormattedNow());
        
        builder.Append(FormattedThreadInfo(levelInfo));
        builder.Append(": ");
        builder.Append(obj is null ? "Null" : obj.ToString()!);
        
        _fileLogger.EnqueueLog(Parser.FromBeautified(builder.ToString(), false));

        builder.AppendLine();
        _stdLogger.Write(Parser.FromBeautified(builder.ToString(), true));
    }

    public static string MakeBanner(ReadOnlySpan<char> text)
    {
        return Richtext.MakeBanner(BANNER_COLOR, text);
    }
    
#if DEBUG
    private static void InternalLogDebug(LevelInfo levelInfo, object? obj)
    {
        const string FILE_NAME_COLOR = "#26BAFF";
        const string ALT_FILE_NAME_COLOR = "#134A63";
        const string FILE_LINE_COLOR = "#2878D4";

        StringBuilder builder = new();
        builder.Append(FormattedNow());

        StackFrame frame = new (
            skipFrames: 2, 
            needFileInfo: true
        );
        string file = frame.GetFileName()!.Split('\\', '/').Last();
        int line = frame.GetFileLineNumber();

        builder.Append(FormattedThreadInfo(levelInfo));
        builder.Append($"|{ALT_FILE_NAME_COLOR},@||{FILE_NAME_COLOR},{file}||{ALT_FILE_NAME_COLOR},:||{FILE_LINE_COLOR},{line}|: ");
        builder.Append(obj is null ? "Null" : obj.ToString()!);
        
        _fileLogger.EnqueueLog(Parser.FromBeautified(builder.ToString(), false));

        builder.AppendLine();
        _stdLogger.Write(Parser.FromBeautified(builder.ToString(), true));
        
    }
#endif

    public static void Put(object? obj)
    {
        StringBuilder builder = new();
        builder.Append(obj is null ? "Null" : obj.ToString()!);
        _fileLogger.EnqueueLog(Parser.FromBeautified(builder.ToString(), false));
        builder.AppendLine();
        _stdLogger.Write(Parser.FromBeautified(builder.ToString(), true));
    }

    public static void Trace(object? obj)
    {
#if DEBUG
        InternalLogDebug(TraceInfo, obj);
#endif
    }
    
    public static void Info(object? obj) => InternalLog(InfoInfo, obj);
    public static void Warning(object? obj) => InternalLog(WarningInfo, obj);
    public static void Error(object? obj) => InternalLog(ErrorInfo, obj);
    public static void Fatal(object? obj) => InternalLog(FatalInfo, obj);
}