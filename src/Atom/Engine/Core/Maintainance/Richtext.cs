namespace Atom;

public static class Richtext
{
    public const char Escape = '\\';
    public const string Bold = "**";
    public const string Italic = "*";
    public const string ItalicAlternative = "_";
    public const string Underline = "__";
    public const string DoubleUnderline = "___";
    public const string Strike = "~~";
    public const string RapidBlink = "!!!";
    public const string SlowBlink = "!!";
    public const string Raw = "`";

    public const string ColorInvert = "|||";
    public const string Color = "|";

    
    public const string ColorBackgroundReset = "^^^";
    public const string ColorForegroundReset = "^^";
    public const string ColorReset = "^";

    public static class ColorCode
    {
        public const string PATH = "#26BAFF";
        public const string URL = PATH;
        public const string AT = "#134A63";
        public const string FAINT = "#363636";
        public const string MEMORY_ADDRESS = "#FF8000";
        public const string MEMORY_ADDRESS_FAINT = "#693400";
    }


    public static string MakeBanner(ReadOnlySpan<char> bannerColor, ReadOnlySpan<char> text)
    {
        return $"|{bannerColor},[|{text}|{bannerColor},]|";
    }
}