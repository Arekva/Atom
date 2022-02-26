using System.Reflection;

namespace Atom.Engine;

public static class ImageFormatMapper
{
    private static Dictionary<Type, ImageFormat> _photonToVkMap;
    private static Dictionary<ImageFormat, Type> _vkToPhotonMap;
    

    public static Type Map(ImageFormat vkImageFormat) => _vkToPhotonMap[vkImageFormat];
    public static ImageFormat Map(Type dataFormat) => _photonToVkMap[dataFormat];

    static ImageFormatMapper()
    {
        _photonToVkMap = new Dictionary<Type, ImageFormat>(capacity: 255);
        _vkToPhotonMap = new Dictionary<ImageFormat, Type>(capacity: 255);
        
        AddOrMapping(Assembly.GetExecutingAssembly());
    }

    public static void AddOrMapping(Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        
        IEnumerable<Type> imageFormatTypes = assembly
            .GetTypes()
            .Where(type => type.IsAssignableTo(typeof(IImageFormat)));
        
        foreach (Type formatType in imageFormatTypes)
        {
            if (formatType == typeof(IImageFormat)) continue;
            
            ImageFormatAttribute? formatAttribute = formatType.GetCustomAttribute<ImageFormatAttribute>();

            if (formatAttribute == null)
            {
                Log.Error($"Image data type {formatType.Name} has no {nameof(ImageFormatAttribute)} defined.");
            }
            else
            {
                if (_photonToVkMap.ContainsKey(formatType))
                {
                    _photonToVkMap[formatType] = formatAttribute.Format;
                    _vkToPhotonMap[formatAttribute.Format] = formatType;
                }
                else
                {
                    _photonToVkMap.Add(formatType, formatAttribute.Format);
                    _vkToPhotonMap.Add(formatAttribute.Format, formatType);
                }
            }
        }
    }
}