using System.Globalization;
using System.Text.Json.Serialization;

namespace Atom.Engine;

public struct Version : IComparable<Version>, IEquatable<Version>, IFormattable
{
    public static Version MajorRelease { get; } = new(1, 0, 0);
    public static Version MinorRelease { get; } = new(0, 1, 0);
    public static Version Fix { get; } = new(0, 0, 1);

    private const string DefaultFormat = "{n} {M}.{m}.{p}";
    
    [JsonIgnore] public string? Name { get; }
    public uint Major { get; }
    public uint Minor { get; }
    public uint Patch { get; }
    [JsonIgnore] public uint Build { get; }
    

    [JsonConstructor] public Version(uint major, uint minor, uint patch) : this(null, major, minor, patch) { }
    public Version(string? name, uint major, uint minor, uint patch) : this(name, major, minor, patch, 0) { }
    public Version(string? name, uint major, uint minor, uint patch, uint build)
    {
        this.Name = name;
        this.Major = major;
        this.Minor = minor;
        this.Patch = patch;
        this.Build = build;
    }
    
    public Version(uint vkVersion)
    {
        uint vk = vkVersion;
        Major = (vk >> 22) & 0x7FU;
        Minor = (vk >> 12) & 0x3FFU;
        Patch = vk & 0xFFFU;
        Build = vk >> 29;
        Name = null;
    }
    public Version(string? name, uint vkVersion)
    {
        uint vk = vkVersion;
        Major = (vk >> 22) & 0x7FU;
        Minor = (vk >> 12) & 0x3FFU;
        Patch = vk & 0xFFFU;
        Build = vk >> 29;
        Name = name;
    }
    
    public static uint GetApiVersion(Version version) =>
        GetApiVersion(version.Build, version.Major, version.Minor, version.Patch);
    public static uint GetApiVersion(uint variant, uint major, uint minor, uint patch)
        => variant << 29 | major << 22 | minor << 12 | patch;

    public override bool Equals(object? obj) => obj is Version other && Equals(other);
    public bool Equals(Version other) => other.Major == Major && other.Minor == Minor && other.Patch == Patch;

    public int CompareTo(Version other)
    {
        if (other.Major < this.Major) return  1;
        if (other.Major > this.Major) return -1;
        
        if (other.Minor < this.Minor) return  1;
        if (other.Minor > this.Minor) return -1;
        
        if (other.Patch < this.Patch) return  1;
        if (other.Patch > this.Patch) return -1;

        return 0;
    }

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

    public override string ToString() => ToString(DefaultFormat);
    public string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public string ToString(string? format, IFormatProvider? provider)
    {
        format ??= DefaultFormat;
        provider ??= CultureInfo.CurrentCulture;
        // don't care about provider, lol.
        
        return format.
            Replace("{n}", Name).
            Replace("{M}", Major.ToString()).
            Replace("{m}", Minor.ToString()).
            Replace("{p}", Patch.ToString()).
            Replace("{b}", Build.ToString()).
            Trim();
    }

    public static bool operator >(Version a, Version b) => a.CompareTo(b) > 0;
    public static bool operator <(Version a, Version b) => a.CompareTo(b) < 0;

    //public static implicit operator Version32(Version version) => new(version.Major, version.Minor, version.Patch);
    public static implicit operator uint(Version version) => version.Build << 29 | version.Major << 22 | version.Minor << 12 | version.Patch;

    public static Version operator *(Version v, uint m) => new(v.Name, v.Major * m, v.Minor * m, v.Patch * m);
    public static Version operator +(Version a, Version b) => new(a.Name, a.Major + b.Major, a.Minor + b.Minor, a.Patch + b.Patch);

    // public static implicit operator Carbon.Version(Atom.Version v) => Unsafe.As<Atom.Version, Carbon.Version>(ref v);
    // public static implicit operator Atom.Version(Carbon.Version v) => Unsafe.As<Carbon.Version, Atom.Version>(ref v);
}