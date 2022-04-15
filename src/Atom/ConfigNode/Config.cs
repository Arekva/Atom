using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Atom.Engine;
using Atom.Engine.Vulkan;
using Silk.NET.Vulkan;

namespace Atom.Game.Config;

public class Config
{
    private static HashSet<char> NUMBER_CHAR = new()
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    private static Dictionary<string, string> GetBindings(string path)
    {
        string[] lines = File.ReadAllLines(path);

        // KVP<BindPoint, Content>
        Dictionary<string, string> data = new();

        bool group_value_assignment = false;
        StringBuilder group_value_data = new(string.Empty);

        Stack<string> current_bindgroup = new(capacity: 8);

        void remove_last_bindpoint()
        {
            _ = current_bindgroup.Pop();
        }

        void add_bindpoint(string binding)
        {
            current_bindgroup.Push(binding);
        }

        void add_data(string content)
        {
            data.Add(string.Join('/', current_bindgroup.Reverse()), content);
            remove_last_bindpoint();
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = Regex.Replace(
                lines[i].Trim().Replace('\t', ' ')
                , @"\s+", " ");

            if (string.IsNullOrEmpty(line)) continue;

            int comment_index = line.IndexOf("//", StringComparison.Ordinal);

            if (comment_index != -1)
            {
                line = line[..comment_index];
            }

            if (string.IsNullOrEmpty(line)) continue; // recheck

            char last_char = line[^1];

            if (last_char == '}')
            {
                if (group_value_assignment)
                {
                    add_data(group_value_data.ToString());
                    group_value_data.Clear();
                    group_value_assignment = false;
                }
                else // end of group bindpoint
                {
                    remove_last_bindpoint();
                }
            }
            else if (last_char == '{')
            {
                // if group value or dictionary assignment
                if (line[^3] == '=')
                {
                    group_value_assignment = true;
                    add_bindpoint(line[..^4]);
                }
                else
                {
                    add_bindpoint(line[..^2]);
                }
            }
            else
            {
                if (group_value_assignment)
                {
                    group_value_data.AppendLine(line);
                }
                else
                {
                    string[] kvp = line.Split('=');
                    add_bindpoint(kvp[0].TrimEnd());
                    add_data(kvp[1].TrimStart());
                }
            }
        }

        return data;
    }
    
    public static T LoadInto<T>(string path) where T : new()
    {
        Dictionary<string, string> bindings = GetBindings(path);

        T filled_config = new();

        foreach ((string path_to_fill, string what_to_fill) in bindings)
        { // i'm tired it's time to give shit but good names (yes it's possible) to my variables.
            (PropertyInfo where_to_fill, object object_to_fill, BindAttribute bind) = GetPropertyForBinding(path_to_fill.Split('/'), filled_config);

            if (where_to_fill.PropertyType == typeof(string))
            {
                where_to_fill.SetValue(object_to_fill, what_to_fill);
            }
            else if (where_to_fill.PropertyType == typeof(f64))
            {
                int length = what_to_fill.Length;

                int boundary = -1;

                for (int i = length-1; i >= 0; i--)
                {
                    if (NUMBER_CHAR.Contains(what_to_fill[i]))
                    {
                        if (i == length - 1) break;
                        boundary = i + 1;
                        break;
                    }
                }
                
                const NumberStyles STYLE =  NumberStyles.AllowDecimalPoint | 
                                            NumberStyles.AllowThousands    |
                                            NumberStyles.AllowExponent     ;

                double final_value = 0.0;
                if (boundary == -1 && bind!.DataType == DataType.Unknown)
                {
                    if (!double.TryParse(what_to_fill, STYLE, CultureInfo.InvariantCulture, out final_value))
                    {
                        throw new Exception($"Cannot parse {what_to_fill} as {typeof(double)}.");
                    }
                }
                else
                {
                    ReadOnlySpan<char> number = what_to_fill[..boundary];
                    ReadOnlySpan<char> unit   = what_to_fill[boundary..];

                    double multiplier = 0.0;
                    try
                    {
                        multiplier = BindAttribute.BASE_UNIT_MAPPERS[bind!.DataType][new string(unit)];
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"No unit {new string(unit)} in the {bind!.DataType} group (binding {path_to_fill})");
                    }
                
                    if (!double.TryParse(number, STYLE, CultureInfo.InvariantCulture, out f64 data))
                    {
                        throw new Exception($"Cannot parse {new string(number)} as {bind!.DataType}.");
                    }
                    
                    final_value = data * multiplier;
                }
                
                where_to_fill.SetValue(object_to_fill, final_value);
            }
        }

        {
            
        }
        return filled_config;
    }

    private static (PropertyInfo property, object obj, BindAttribute? bind) GetPropertyForBinding(string[] binding, object fromObject, int depth = 1)
    {
        const BindingFlags PUBLIC_PROPERTY = BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.Public;
        
        string bind_name = binding[depth];

        BindAttribute? attribute = null;

        PropertyInfo? property = fromObject.GetType()
            .GetProperties(PUBLIC_PROPERTY)
            .FirstOrDefault(p => (attribute = p.GetCustomAttribute<BindAttribute>())?.Point == bind_name);

        if (property == null)
        {
            throw new Exception($"Binding {bind_name} not found in {string.Join('/', binding, 0, depth + 1)}");
        }

        if (depth == binding.Length - 1) 
        {    // if binding has been found
            return (property, fromObject, attribute);
        }
        else // if need to get deeper
        {
            object? property_value = property.GetValue(fromObject);
            if (property_value == null)
            {   // create instance if null, this leaves the user the choice
                // to create a type from other config values before if required.
                property_value = Activator.CreateInstance(property.PropertyType);
                property.SetValue(fromObject, property_value);
            }
            return GetPropertyForBinding(binding, property_value, depth + 1);
        }
    }
}