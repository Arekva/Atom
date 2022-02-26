namespace Atom.Engine;

[AttributeUsage(AttributeTargets.Struct)]
public class ImageFormatAttribute : Attribute
{
    public ImageFormat Format { get; }

    public ImageFormatAttribute(ImageFormat imageFormat) => Format = imageFormat;
}