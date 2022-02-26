using System.Collections.Concurrent;
using Atom.Engine.DDS;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public partial class StandardImage
{
    private static ConcurrentDictionary<(Type baseImageType, ImageFormat dataFormat), Type> _proceduralImageTypes 
        = new ();

    internal static Type GetProceduralType(Type imageType, ImageFormat dataFormat)
    {
        (Type, ImageFormat) tuple = (imageType, dataFormat);

        if (!_proceduralImageTypes.TryGetValue(tuple, out Type? genericType))
        {
            genericType = imageType.MakeGenericType(typeArguments: ImageFormatMapper.Map(dataFormat));
            
            _proceduralImageTypes.TryAdd(tuple, genericType);
        }
        
        return genericType;
    }


    public static IImage FromFile(Device device, uint transferQueue, string filePath)
    {

        /*IImage result = DDSLoader.Load(
            device, 
            transferQueue: transferQueue,
            stream: File.OpenRead(filePath));

        return result;*/

        return null;
    }
}