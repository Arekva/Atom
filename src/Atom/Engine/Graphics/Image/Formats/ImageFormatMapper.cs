using System.Reflection;

namespace Atom.Engine;

public static class ImageFormatMapper
{
    private static Dictionary<Type, ImageFormat> _atomToVkMap;
    private static Dictionary<ImageFormat, Type> _vkToAtomMap;
    

    public static Type Map(ImageFormat vkImageFormat) => _vkToAtomMap[vkImageFormat];
    public static ImageFormat Map(Type dataFormat) => _atomToVkMap[dataFormat];

    static ImageFormatMapper()
    {
        _atomToVkMap = new Dictionary<Type, ImageFormat>(capacity: 255);
        _vkToAtomMap = new Dictionary<ImageFormat, Type>(capacity: 255);
        
        AddToMapping(Assembly.GetExecutingAssembly());
    }

    public static void AddToMapping(Assembly assembly)
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
                if (_atomToVkMap.ContainsKey(formatType))
                {
                    _atomToVkMap[formatType] = formatAttribute.Format;
                    _vkToAtomMap[formatAttribute.Format] = formatType;
                }
                else
                {
                    _atomToVkMap.Add(formatType, formatAttribute.Format);
                    _vkToAtomMap.Add(formatAttribute.Format, formatType);
                }
            }
        }
    }
}