using System.Globalization;
using System.Text;

namespace Atom.Engine.ANSI;

public static class CTRL
{
    /// <summary> ^G - Bell. </summary>
    public const char BEL = '\u0007';
    /// <summary> ^H - Backspace. </summary>
    public const char BS =  '\u0008';
    /// <summary> ^I - Tab. </summary>
    public const char HT =  '\u0009';
    /// <summary> ^J - Line Feed. </summary>
    public const char LF =  '\u000A';
    /// <summary> ^L - Form Feed. </summary>
    public const char FF =  '\u000C';
    /// <summary> ^M - Carriage Return. </summary>
    public const char CR =  '\u000D';
    /// <summary> ^[ - Escape. </summary>
    public const char ESC = '\u001B';

    
    public const char END = 'm';
}

public static class ESC
{
    /// <summary> ESC N - Single Shift Two. </summary>
    public const char SS2 = '\u008E';
    /// <summary> ESC O - Single Shift Three. </summary>
    public const char SS3 = '\u008F';
    /// <summary> ESC P - Device Control String. </summary>
    public const char DCS = '\u0090';
    /// <summary> ESC [ - Control Sequence Introducer. </summary>
    public const char CSI = '[';
    /// <summary> ESC \ - String Terminator. </summary>
    public const char ST = '\u009C';
    /// <summary> ESC ] - Operating System Command. </summary>
    public const char OSC = '\u009D';
    /// <summary> ESC X - Start of String. </summary>
    public const char SOS = '\u0098';
    /// <summary> ESC ^ - Privacy Message. </summary>
    public const char PM = '\u009E';
    /// <summary> ESC _ - Application Program Command. </summary>
    public const char APC = '\u009F';
}

public static class CSI
{
    public const byte Reset = 0;
    public const byte Bold = 1;
    public const byte Faint = 2;
    public const byte Italic = 3;
    public const byte Underline = 4;
    public const byte SlowBlink = 5;
    public const byte RapidBlink = 6;
    
    public const byte DoublyUnderlined = 21;
    public const byte NormalIntensity = 22;

}

public static class Parser
{
    private enum FormatType
    {
        Bold,
        Italic,
        DoubleUnderline,
        Underline,
        Strike,
        RapidBlink,
        SlowBlink,
        Raw,
        ColorInvert,
        Color,
        ColorBackgroundReset,
        ColorForegroundReset,
        ColorReset
    }

    private struct Format
    {
        public FormatType Type;
        public string StartAnsiCode;
        public string EndAnsiCode;
        public string Markdown;
        public int Index;
    }

    private static readonly Format[] _formats =
    {
        new () /* Bold '**' */
        {
            Type = FormatType.Bold,
            StartAnsiCode = "\u001B[1m",
            EndAnsiCode = "\u001B[22m",
            Markdown = Richtext.Bold,
            Index = 0,
        },
        new () /* Italic '*' */
        {
            Type = FormatType.Italic,
            StartAnsiCode = "\u001B[3m",
            EndAnsiCode = "\u001B[23m",    
            Markdown = Richtext.Italic,
            Index = 1,
        },
        new () /* Double underline '___' */
        {
            Type = FormatType.DoubleUnderline,
            StartAnsiCode = "\u001B[21m",
            EndAnsiCode = "\u001B[24m",
            Markdown = Richtext.DoubleUnderline,
            Index = 2,
        },
        new () /* Underline '__' */
        {
            Type = FormatType.Underline,
            StartAnsiCode = "\u001B[4m",
            EndAnsiCode = "\u001B[24m",
            Markdown = Richtext.Underline,
            Index = 3,
        },
        new () /* Italic '_' */
        {
            Type = FormatType.Italic,
            StartAnsiCode = "\u001B[3m",
            EndAnsiCode = "\u001B[23m",
            Markdown = Richtext.ItalicAlternative,
            Index = 4,
        },
        new () /* Stroke '~~' */
        {
            Type = FormatType.Strike,
            StartAnsiCode = "\u001B[9m",
            EndAnsiCode = "\u001B[29m",
            Markdown = Richtext.Strike,
            Index = 5,
        },
        new () /* Rapid blink '!!' */
        {
            Type = FormatType.RapidBlink,
            StartAnsiCode = "\u001B[6m",
            EndAnsiCode = "\u001B[25m",
            Markdown = Richtext.RapidBlink,
            Index = 6,
        },
        new () /* Slow blink '!' */
        {
            Type = FormatType.SlowBlink,
            StartAnsiCode = "\u001B[5m",
            EndAnsiCode = "\u001B[25m",
            Markdown = Richtext.SlowBlink,
            Index = 7,
        },
        new () /* Raw '`' */
        {
            Type = FormatType.Raw,
            StartAnsiCode = null!,  // script controlled
            EndAnsiCode = null!,    // script controlled
            Markdown = Richtext.Raw,
            Index = 8,
        },
        new () /* Foreground <=> Background color invert '||' */
        {
            Type = FormatType.ColorInvert,
            StartAnsiCode = "\u001B[7m",
            EndAnsiCode = "\u001B[27m",
            Markdown = Richtext.ColorInvert,
            Index = 9,
        },
        new () /* Color set '|#foregroundColor,#backgroundColor..|' or '|#foregroundColor,..|' or '|,#backgroundColor'..| */
        {
            Type = FormatType.Color,
            StartAnsiCode = null!,  // script controlled
            EndAnsiCode = "\u001B[39m\u001B[49m",
            Markdown = Richtext.Color,
            Index = 10,
        },
        new () /* Color background reset '^^^' */
        {
            Type = FormatType.ColorBackgroundReset,
            StartAnsiCode = "\u001B[49m",
            EndAnsiCode = "\u001B[49m",
            Markdown = Richtext.ColorBackgroundReset,
            Index = 11,
        },
        new () /* Color background reset '^^' */
        {
            Type = FormatType.ColorForegroundReset,
            StartAnsiCode = "\u001B[39m",
            EndAnsiCode = "\u001B[39m",
            Markdown = Richtext.ColorForegroundReset,
            Index = 12,
        },
        new () /* Color reset '^' */
        {
            Type = FormatType.ColorReset,
            StartAnsiCode = "\u001B[39m\u001B[49m",
            EndAnsiCode = "\u001B[39m\u001B[49m",
            Markdown = Richtext.ColorReset,
            Index = 13,
        },
    };
    
    /*[Unmaintainable(
        "Ok so this is terrible." +
        " Well, not everything is hacky but the colors and raw input (at least) are."
    )]*/
    public static string FromBeautified(ReadOnlySpan<char> beautified, bool ansiCompatibleMode)
    {
        int index = 0;
        int length = beautified.Length;

        int typesCount = _formats.Length;
        
        StringBuilder builder = new(capacity: beautified.Length);
        
        int IndexOfMarkdown(ReadOnlySpan<char> text, int fromIndex, bool rawOnly, out Format format, out bool isEscaped)
        {
            int minIndexInString = int.MaxValue;
            int formatIndex = -1;
            bool shouldBeIgnored = isEscaped = false;
            
            format = default;

            if (rawOnly)
            {
                int index = text[fromIndex..].IndexOf(Richtext.Raw);
                int globalIndex = index + fromIndex;
                if (index != -1 && globalIndex < minIndexInString)
                {
                    // found
                    if (globalIndex > 0 && text[globalIndex - 1] == Richtext.Escape)
                    {
                        shouldBeIgnored = true;
                    }
                    minIndexInString = globalIndex;
                    formatIndex = 8;
                }
            }
            else
            {
                for (int i = 0; i < _formats.Length; i++)
                {
                    int index = text[fromIndex..].IndexOf(_formats[i].Markdown);
                    int globalIndex = index + fromIndex;
                    if (index != -1 && globalIndex < minIndexInString)
                    { // found
                        if (globalIndex > 0 && text[globalIndex - 1] == Richtext.Escape)
                        {
                            shouldBeIgnored = true;
                        }
                    
                        minIndexInString = globalIndex;
                        formatIndex = i;
                    }
                }
            }
            

            if (minIndexInString != int.MaxValue)
            {
                isEscaped = shouldBeIgnored;
                format = _formats[formatIndex];

                return minIndexInString;
            }
            
            
            return -1; // not found :(
        }
        
        Span<bool> states = stackalloc bool[typesCount];

        int remainingCount = length;
        while ((remainingCount = length - index) != 0)
        {
            int nextMarkdown = IndexOfMarkdown(beautified, index, states[8], out Format format, out bool escaped);

            if (nextMarkdown == -1)
            { // no markdown available anymore, append everything remaining.
                builder.Append(beautified[index..]);
                index = length;
            }
            else
            { // markdown has been found, get it
                int nextMarkdownStartIndex = nextMarkdown;
                int nextMarkdownEndIndex = nextMarkdownStartIndex + format.Markdown.Length;

                int textLengthUntilNextMarkdown = nextMarkdownStartIndex - index;
                
                // append any text previous-ing the markdown and apply the markdown (or not if escaped).
                
                if (escaped)
                { // append everything before but the escape
                    builder.Append(beautified[index..(nextMarkdownStartIndex - 1)]);
                    // and then the markdown code
                    builder.Append(beautified[nextMarkdownStartIndex]);
                    index += textLengthUntilNextMarkdown + 1; // the escape code is a char, so 1 of length
                }
                else if (format.Type == FormatType.Raw) // raw input: skip everything within the codes
                {
                    // append everything before
                    builder.Append(beautified[index..nextMarkdownStartIndex]);

                    states[format.Index] = !states[format.Index];

                    index += textLengthUntilNextMarkdown + format.Markdown.Length;
                }
                else if (format.Type == FormatType.Color)
                {
                    // append everything before
                    builder.Append(beautified[index..nextMarkdownStartIndex]);
                    index += (nextMarkdownEndIndex - index);

                    if (!escaped)
                    {
                        if (!states[format.Index] && nextMarkdownEndIndex != length)
                        {
                            const string FOREGROUND = "38;2;";
                            const string BACKGROUND = "48;2;";

                            char firstChar = beautified[nextMarkdownEndIndex];

                            void SetColor(ReadOnlySpan<char> ansiMode, ReadOnlySpan<char> colorCode)
                            {
                                if (ansiCompatibleMode &&
                                    int.TryParse(colorCode, NumberStyles.HexNumber, null, out int code))
                                {
                                    // code is valid, insert in output
                                    builder.Append("\u001B[");
                                    builder.Append(ansiMode);
                                    for (int i = 2; i > -1; i--)
                                    {
                                        builder.Append((code >> i * 8) & 0xFF);
                                        if (i != 0)
                                        {
                                            builder.Append(';');
                                        }
                                    }

                                    builder.Append('m');
                                }
                            }

                            dirty_code_check_again_first_char:
                            if (firstChar == ',') // directly switch to background setter
                            {
                                index += 1;
                                if (length - index != 0)
                                {
                                    if (beautified[index] == '#')
                                    {
                                        index += 1;
                                        if (length - index >= 6) // if has enough space to even have a code
                                        {
                                            SetColor(BACKGROUND, beautified[index..(index + 6)]);
                                            index += 6;
                                            states[format.Index] = true;
                                        }
                                    }
                                    else if (beautified[index] == Richtext.Escape)
                                    {
                                        index += 1; // remove escape
                                    }
                                    else ; // not background color.
                                }

                            }
                            else if (firstChar == '#') // this is a foreground color
                            {
                                if (length - index != 0)
                                {
                                    if (beautified[index] == '#')
                                    {
                                        index += 1;
                                        if (length - index >= 6) // if has enough space to even have a code
                                        {
                                            SetColor(FOREGROUND, beautified[index..(index + 6)]);
                                            index += 6;
                                            states[format.Index] = true;
                                            
                                            if (index < length) // check for background
                                            {
                                                firstChar = beautified[index];
                                                goto dirty_code_check_again_first_char;
                                            }
                                        }
                                    }
                                    else if (beautified[index] == Richtext.Escape)
                                    {
                                        index += 1; // remove escape
                                    }
                                }
                            }
                            else ; // not a color, sad.
                        }
                        else if (states[format.Index])
                        {
                            if (ansiCompatibleMode)
                            {
                                builder.Append(format.EndAnsiCode);
                            }

                            states[format.Index] = false;
                        }
                    }
                }
                else
                {
                    builder.Append(beautified[index..nextMarkdownStartIndex]);

                    index += textLengthUntilNextMarkdown + format.Markdown.Length;

                    if (ansiCompatibleMode)
                    {
                        if (states[format.Index])
                        {
                            // if ANSI formatting for this markdown active, disable it
                            builder.Append(format.EndAnsiCode);
                            states[format.Index] = false;
                        }
                        else // otherwise enable it
                        {
                            builder.Append(format.StartAnsiCode);
                            states[format.Index] = true;
                        }
                    }
                }
            }
        }
        
        // if any formatting is still active, disable it
        if (ansiCompatibleMode)
        {
            for (int i = 0; i < typesCount; i++)
            {
                if (states[i])
                {
                    builder.Append(_formats[i].EndAnsiCode);
                }
            }
        }

        return builder.ToString();
    }
}