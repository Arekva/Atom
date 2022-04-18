using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Atom.Engine;
using Silk.NET.Maths;

namespace Atom.Game.Config;

public class ConfigFile
{
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

    private static Location ParseLocation(string location, BindAttribute bind, string path)
    {
        const NumberStyles DOUBLE_STYLE =  NumberStyles.AllowDecimalPoint | 
                                           NumberStyles.AllowThousands    |
                                           NumberStyles.AllowExponent     ;
        
        const NumberStyles INT_STYLE    =  NumberStyles.Integer           |
                                           NumberStyles.AllowThousands    ;
        
        

        string[] sector_coordinates = location.Split('<');

        string[] sector_comps = sector_coordinates[0].Split(';').Select(s => s.Trim()).ToArray();
        string[] coords_comps = sector_coordinates[1].Split(';').Select(s => s.Trim()).ToArray();

        return new Location(
            coordinates: new Vector3D<f64>(
                ParseDouble(coords_comps[0]      , bind, path),
                ParseDouble(coords_comps[1]      , bind, path),
                ParseDouble(coords_comps[2][..^1], bind, path)
                /*double.Parse(coords_comps[0]               , DOUBLE_STYLE, CultureInfo.InvariantCulture),
                double.Parse(coords_comps[1]               , DOUBLE_STYLE, CultureInfo.InvariantCulture),
                double.Parse(coords_comps[2].AsSpan()[..^1], DOUBLE_STYLE, CultureInfo.InvariantCulture)*/
            ),
            sector: new Vector3D<i64>(
                long.Parse(sector_comps[0].AsSpan()[1..] , INT_STYLE, CultureInfo.InvariantCulture),
                long.Parse(sector_comps[1]               , INT_STYLE, CultureInfo.InvariantCulture),
                long.Parse(sector_comps[2].AsSpan()[..^1], INT_STYLE, CultureInfo.InvariantCulture)
            )
        );
    }

    private static double ParseDouble(string @double, BindAttribute bind, string path)
    {
        string[] number_unit = @double.Split(' ');
        
        const NumberStyles STYLE =  NumberStyles.AllowDecimalPoint | 
                                    NumberStyles.AllowThousands    |
                                    NumberStyles.AllowExponent     ;

        f64 final_value = 0.0;
        if (number_unit.Length < 2 && bind.DataType == DataType.Unknown)
        {
            if (!double.TryParse(@double, STYLE, CultureInfo.InvariantCulture, out final_value))
            {
                throw new Exception($"Cannot parse {@double} as {typeof(double)}.");
            }
        }
        else
        {
            ref readonly string number = ref number_unit[0];
            ref readonly string unit   = ref number_unit[1];

            f64 multiplier = 0.0;
            try
            {
                multiplier = BindAttribute.BASE_UNIT_MAPPERS[bind!.DataType][new string(unit)];
            }
            catch (Exception e)
            {
                throw new Exception($"No unit {new string(unit)} in the {bind!.DataType} group (binding {path})");
            }
        
            if (!double.TryParse(number, STYLE, CultureInfo.InvariantCulture, out f64 data))
            {
                throw new Exception($"Cannot parse {new string(number)} as {bind!.DataType}.");
            }
            
            final_value = data * multiplier;
        }

        return final_value;
    }
    
    private static object ParseData(Type dataType, string data, BindAttribute bind, string path)
    {
        if (dataType == typeof(string))
        {
            return data;
        }
        else if (dataType == typeof(f64))
        {
            return ParseDouble(data, bind, path);
        }
        else if (dataType == typeof(Location))
        {
            return ParseLocation(data, bind, path);
        }
        else
        {
            throw new NotImplementedException($"Data type {dataType} has no available parser.");
        }
    }

    private static void SetDictionary(PropertyInfo property, object @object, string kvps, BindAttribute bind, string path)
    {
        // get the dictionary adder
        object? value_dictionary = property.GetValue(@object);
        if (value_dictionary == null)
        {
            value_dictionary = Activator.CreateInstance(property.PropertyType);
            property.SetValue(@object, value_dictionary);
        }

        const BindingFlags PUBLIC_METHOD = BindingFlags.Instance    | 
                                           BindingFlags.Public      | 
                                           BindingFlags.InvokeMethod;
        MethodInfo dic_add_method = value_dictionary!.GetType().GetMethod("TryAdd", PUBLIC_METHOD)!;
        
        // get map method
        Type[] generic_types = property.PropertyType.GetGenericArguments();

        const BindingFlags PRIVATE_STATIC_FIELD = BindingFlags.Static      | 
                                                  BindingFlags.NonPublic   | 
                                                  BindingFlags.InvokeMethod;
        MethodInfo? map = generic_types[0].GetMethod("GetMapValue", PRIVATE_STATIC_FIELD);

        if (map == null)
        {
            throw new Exception("Dictionary mapper types must have a \"private static T GetMapValue(string)\" method.");
        }
        
        object[] added_kvp     = new object[2];
        object[] key_retriever = new object[1];

        foreach (string kvp in kvps.Split("\r\n"))
        {
            if (string.IsNullOrEmpty(kvp)) continue;
            
            string[] key_value = kvp.Split('=').Select(s => s.Trim()).ToArray();
            key_retriever[0] = key_value[0];
            
            
            // get the object to map the final with
            object key = map.Invoke(null, key_retriever)!;
            object value = ParseData(generic_types[1], key_value[1], bind, path);

            added_kvp[0] = key  ;
            added_kvp[1] = value;
            
            dic_add_method.Invoke(value_dictionary, added_kvp);
        }
    }
    
    public static T LoadInto<T>(string path) where T : new()
    {
        Dictionary<string, string> bindings = GetBindings(path);

        T filled_config = new();

        foreach ((string path_to_fill, string what_to_fill) in bindings)
        {
            try
            {
                // i'm tired it's time to give shit but good names (yes it's possible) to my variables.
                (PropertyInfo where_to_fill, object object_to_fill, BindAttribute bind) =
                    GetPropertyForBinding(path_to_fill.Split('/'), filled_config);

                Type property_type = where_to_fill.PropertyType;

                if (property_type.IsGenericType && property_type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    SetDictionary(where_to_fill, object_to_fill, what_to_fill, bind!, path_to_fill);
                }
                else
                {
                    where_to_fill.SetValue(object_to_fill, ParseData(property_type, what_to_fill, bind!, path_to_fill));
                }
            }
            catch (BindNotFoundException)
            {
                Log.Warning($"{typeof(T).Name}: no bind corresponding found at {path_to_fill}");
            }
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
            throw new BindNotFoundException($"Binding {bind_name} not found in {string.Join('/', binding, 0, depth + 1)}");
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
            return GetPropertyForBinding(binding, property_value!, depth + 1);
        }
    }
}

[Serializable]
public class BindNotFoundException : Exception
{
    public BindNotFoundException()
    {
    }

    public BindNotFoundException(string message) : base(message)
    {
    }

    public BindNotFoundException(string message, Exception inner) : base(message, inner)
    {
    }

    protected BindNotFoundException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}