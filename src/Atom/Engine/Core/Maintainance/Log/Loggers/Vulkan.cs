using System.Text;
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

        u32 relevant_objects_count = vkData->ObjectCount;
        ReadOnlySpan<vk.DebugUtilsObjectNameInfoEXT> relevant_objects = new(vkData->PObjects, (i32)relevant_objects_count);

        string message = msg;
        string? link = null;
        i32 link_start = msg.IndexOf(value: "(http", StringComparison.Ordinal);
        if (link_start != -1)
        {
            link = msg.Substring(link_start + 1, msg.Length - (link_start + 2));
            message = msg.Substring(0, link_start - 1);
        }

        StringBuilder builder = new("[|#FF4400,VK| ");
        builder.Append(type_icon);
        builder.Append("]\n*Message*   : ");
        builder.Append('`');
        builder.Append(message);
        builder.Append('`');
        builder.Append('\n');
        if (link != null)
        {
            builder.Append("\n*Spec. Link*: |");
            builder.Append(Richtext.ColorCode.URL);
            builder.Append(",__`");
            builder.Append(link);
            builder.Append("`__|");
        }

        if (relevant_objects_count != 0U)
        {
            builder.Append("\n*Objects*   : ");
            
            for (i32 i = 0; i < relevant_objects_count; i++)
            {
                ref readonly vk.DebugUtilsObjectNameInfoEXT object_info = ref relevant_objects[i];

                string object_name = 
                    object_info.PObjectName == null ? 
                        "0x" + object_info.ObjectHandle.ToString("X16") :
                        LowLevel.GetString(object_info.PObjectName)!;
            
                builder.Append(object_name);
                builder.Append(" (");
                builder.Append(object_info.ObjectType);
                builder.Append(')');

                if (i + 1 != relevant_objects_count)
                {
                    builder.Append(", ");
                }
            }
        }

        if (link != null || relevant_objects_count > 0U)
        {
            builder.Append('\n');
        }

        SEVERITIES_PFNS[severity](obj: builder);

        return vk.Vk.False;
    }
}