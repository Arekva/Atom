using Atom.Engine;

namespace Atom.Loggers;

public class Vulkan
{
    static readonly Dictionary<DebugUtilsMessageTypeFlags, string> TYPES_ICONS = new()
    {
        { DebugUtilsMessageTypeFlags.General    , "🔧" },
        { DebugUtilsMessageTypeFlags.Performance, "⚡"  },
        { DebugUtilsMessageTypeFlags.Validation , "📝" }
    };
    
    static readonly Dictionary<DebugUtilsMessageSeverityFlags, Action<object?>> SEVERITIES_PFNS = new()
    { 
        { DebugUtilsMessageSeverityFlags.Verbose, Log.Trace   },
        { DebugUtilsMessageSeverityFlags.Info   , Log.Info    },
        { DebugUtilsMessageSeverityFlags.Warning, Log.Warning },
        { DebugUtilsMessageSeverityFlags.Error  , Log.Error   }
    };

    public static unsafe u32 VKLog(vk.DebugUtilsMessageSeverityFlagsEXT vkSeverity, 
        vk.DebugUtilsMessageTypeFlagsEXT vkType, vk.DebugUtilsMessengerCallbackDataEXT* vkData, void* userData)
    {
        DebugUtilsMessageSeverityFlags severity = vkSeverity.ToAtom();
        DebugUtilsMessageTypeFlags     type     = vkType.ToAtom();

        string type_icon = TYPES_ICONS[type];

        string msg = LowLevel.GetString(vkData->PMessage)!.Split('|').Last().TrimStart();

        SEVERITIES_PFNS[severity](obj: $"[|#FF4400,VK| {type_icon}] `{msg}`");

        return vk.Vk.False;
    }
}