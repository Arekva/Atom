using System.Diagnostics;
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

    static readonly List<string> IGNORE_FILTER = new()
    {
        "Unloading layer",
        "Loading layer",
        "loader_add_implicit_layer"
    };

    public static unsafe u32 VKLog(vk.DebugUtilsMessageSeverityFlagsEXT vkSeverity, 
        vk.DebugUtilsMessageTypeFlagsEXT vkType, vk.DebugUtilsMessengerCallbackDataEXT* vkData, void* userData)
    {
        DebugUtilsMessageSeverityFlags severity = vkSeverity.ToAtom();
        DebugUtilsMessageTypeFlags     type     = vkType.ToAtom();

        string type_icon = TYPES_ICONS[type];

        string msg = LowLevel.GetString(vkData->PMessage)!.Split('|').Last().TrimStart();

        foreach (string filter in IGNORE_FILTER)
        {
            if (msg.Contains(filter))
            {
                // skip
                return vk.Vk.False;
            }
        }
        
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
        builder.Append("] ");

        const i32 MAX_STACK_FRAME = 7;
        
        if (new StackFrame(needFileInfo: true, skipFrames: MAX_STACK_FRAME + 1).GetFileName() != null)
        {   
            builder.Append('|');
            builder.Append(Richtext.ColorCode.FAINT);
            builder.Append(',');
            builder.Append("(stackframe not full) / ");
            builder.Append('|');
        }
        
        for (i32 i = MAX_STACK_FRAME; i > 1; --i)
        {
            StackFrame stack_frame = new (needFileInfo: true, skipFrames: i);
            string? file_name = stack_frame.GetFileName();
            if (file_name == null) continue;
            
            builder.Append('|');
            builder.Append(Richtext.ColorCode.PATH);
            builder.Append(',');
            builder.Append(file_name.Split('/', '\\').Last());
            builder.Append("||");
            builder.Append(Richtext.ColorCode.AT);
            builder.Append(",:");
            builder.Append(stack_frame.GetFileLineNumber());
            builder.Append('|');

            if (i != 0)
            {
                builder.Append('|');
                builder.Append(Richtext.ColorCode.FAINT);
                builder.Append(",/|");
            }
        }

        builder.Append("\n*Message*   : ");
        builder.Append(message);
        builder.Append("\n");
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