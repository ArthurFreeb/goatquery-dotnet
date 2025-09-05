using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

internal static class PropertyMappingHelper
{
    public static Dictionary<string, string> CreatePropertyMapping<T>()
    {
        return CreatePropertyMapping(typeof(T));
    }
    
    public static Dictionary<string, string> CreatePropertyMapping(Type type)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var jsonPropertyNameAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonPropertyNameAttribute != null)
            {
                result[jsonPropertyNameAttribute.Name] = property.Name;
                continue;
            }

            result[property.Name] = property.Name;
        }

        return result;
    }
}