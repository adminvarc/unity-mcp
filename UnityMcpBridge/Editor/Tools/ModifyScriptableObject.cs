using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles ScriptableObject property modification operations within the Unity project.
    /// Enhanced with detailed voice attribute inspection capabilities.
    /// </summary>
    public static class ModifyScriptableObject
    {
        /// <summary>
        /// Main handler for ScriptableObject property modification commands
        /// </summary>
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLower();
            string assetPath = @params["asset_path"]?.ToString();
            
            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("Action parameter is required.");
            }
            
            if (string.IsNullOrEmpty(assetPath))
            {
                return Response.Error("asset_path parameter is required.");
            }

            try
            {
                // Load the ScriptableObject asset
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset == null)
                {
                    return Response.Error($"ScriptableObject not found at path: {assetPath}");
                }

                switch (action)
                {
                    case "get_properties":
                        return GetAllProperties(asset);
                    
                    case "get_property":
                        return GetProperty(asset, @params);
                    
                    case "set_property":
                        return SetProperty(asset, @params);
                    
                    case "get_info":
                        return GetScriptableObjectInfo(asset);
                    
                    case "list_array_elements":
                        return ListArrayElements(asset, @params);
                    
                    case "inspect_array_detailed":
                        return InspectArrayDetailed(asset, @params);
                    
                    case "add_array_element":
                        return AddArrayElement(asset, @params);
                    
                    case "remove_array_element":
                        return RemoveArrayElement(asset, @params);
                    
                    case "set_array_element":
                        return SetArrayElement(asset, @params);
                    
                    default:
                        return Response.Error($"Unknown action: {action}. Available actions: get_properties, get_property, set_property, get_info, list_array_elements, inspect_array_detailed, add_array_element, remove_array_element, set_array_element");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModifyScriptableObject] Action '{action}' failed: {e}");
                return Response.Error($"Error handling command: {e.Message}");
            }
        }

        private static object GetAllProperties(ScriptableObject asset)
        {
            try
            {
                var type = asset.GetType();
                var properties = new List<object>();
                
                // Get all serialized fields (public fields and fields with [SerializeField])
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.IsPublic || f.GetCustomAttributes(typeof(SerializeField), false).Length > 0);

                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(asset);
                        var propertyInfo = new
                        {
                            name = field.Name,
                            type = field.FieldType.Name,
                            fullType = field.FieldType.ToString(),
                            value = ConvertValueToString(value),
                            isArray = field.FieldType.IsArray || (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)),
                            isEnum = field.FieldType.IsEnum,
                            isPublic = field.IsPublic
                        };
                        properties.Add(propertyInfo);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to get property {field.Name}: {e.Message}");
                    }
                }

                return Response.Success($"Retrieved {properties.Count} properties from {asset.name}", new
                {
                    assetName = asset.name,
                    typeName = type.Name,
                    properties = properties
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting properties: {e.Message}");
            }
        }
        private static object GetProperty(ScriptableObject asset, JObject @params)
        {
            string propertyName = @params["property_name"]?.ToString();
            if (string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("property_name parameter is required for get_property action.");
            }

            try
            {
                var field = GetField(asset.GetType(), propertyName);
                if (field == null)
                {
                    return Response.Error($"Property '{propertyName}' not found on {asset.GetType().Name}");
                }

                var value = field.GetValue(asset);
                
                return Response.Success($"Retrieved property '{propertyName}' from {asset.name}", new
                {
                    propertyName = propertyName,
                    type = field.FieldType.Name,
                    fullType = field.FieldType.ToString(),
                    value = ConvertValueToString(value),
                    rawValue = value
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting property '{propertyName}': {e.Message}");
            }
        }

        private static object SetProperty(ScriptableObject asset, JObject @params)
        {
            string propertyName = @params["property_name"]?.ToString();
            var propertyValue = @params["property_value"];
            string propertyType = @params["property_type"]?.ToString();
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("property_name parameter is required for set_property action.");
            }
            
            if (propertyValue == null)
            {
                return Response.Error("property_value parameter is required for set_property action.");
            }

            try
            {
                var field = GetField(asset.GetType(), propertyName);
                if (field == null)
                {
                    return Response.Error($"Property '{propertyName}' not found on {asset.GetType().Name}");
                }

                // Convert the value to the appropriate type
                object convertedValue = ConvertValue(propertyValue, field.FieldType, propertyType);
                
                // Set the value
                field.SetValue(asset, convertedValue);
                
                // Mark the asset as dirty so Unity saves the changes
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                return Response.Success($"Successfully set property '{propertyName}' on {asset.name}", new
                {
                    propertyName = propertyName,
                    oldValue = "N/A", // We could store the old value if needed
                    newValue = ConvertValueToString(convertedValue),
                    type = field.FieldType.Name
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Error setting property '{propertyName}': {e.Message}");
            }
        }

        private static object GetScriptableObjectInfo(ScriptableObject asset)
        {
            try
            {
                var type = asset.GetType();
                var assetPath = AssetDatabase.GetAssetPath(asset);
                
                return Response.Success($"Retrieved info for {asset.name}", new
                {
                    name = asset.name,
                    typeName = type.Name,
                    fullTypeName = type.FullName,
                    assetPath = assetPath,
                    instanceId = asset.GetInstanceID(),
                    hideFlags = asset.hideFlags.ToString()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting ScriptableObject info: {e.Message}");
            }
        }
        private static object ListArrayElements(ScriptableObject asset, JObject @params)
        {
            string propertyName = @params["property_name"]?.ToString();
            if (string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("property_name parameter is required for list_array_elements action.");
            }

            try
            {
                var field = GetField(asset.GetType(), propertyName);
                if (field == null)
                {
                    return Response.Error($"Property '{propertyName}' not found on {asset.GetType().Name}");
                }

                var value = field.GetValue(asset);
                if (value == null)
                {
                    return Response.Success($"Property '{propertyName}' is null", new
                    {
                        propertyName = propertyName,
                        count = 0,
                        elements = new object[0]
                    });
                }

                if (value is IList list)
                {
                    var elements = new List<object>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        elements.Add(new
                        {
                            index = i,
                            value = ConvertValueToString(list[i]),
                            type = list[i]?.GetType()?.Name ?? "null"
                        });
                    }

                    return Response.Success($"Listed {list.Count} elements from array '{propertyName}'", new
                    {
                        propertyName = propertyName,
                        count = list.Count,
                        elements = elements
                    });
                }
                else
                {
                    return Response.Error($"Property '{propertyName}' is not an array or list");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error listing array elements for '{propertyName}': {e.Message}");
            }
        }
        private static object InspectArrayDetailed(ScriptableObject asset, JObject @params)
        {
            string propertyName = @params["property_name"]?.ToString();
            if (string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("property_name parameter is required for inspect_array_detailed action.");
            }

            try
            {
                var field = GetField(asset.GetType(), propertyName);
                if (field == null)
                {
                    return Response.Error($"Property '{propertyName}' not found on {asset.GetType().Name}");
                }

                var value = field.GetValue(asset);
                if (value == null)
                {
                    return Response.Success($"Property '{propertyName}' is null", new
                    {
                        propertyName = propertyName,
                        count = 0,
                        elements = new object[0],
                        elementType = "null"
                    });
                }

                if (value is IList list)
                {
                    var elements = new List<object>();
                    Type elementType = null;
                    
                    // Determine element type
                    if (field.FieldType.IsGenericType)
                    {
                        elementType = field.FieldType.GetGenericArguments()[0];
                    }
                    else if (field.FieldType.IsArray)
                    {
                        elementType = field.FieldType.GetElementType();
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        var element = list[i];
                        var elementDetails = new
                        {
                            index = i,
                            type = element?.GetType()?.Name ?? "null",
                            properties = InspectObjectProperties(element)
                        };
                        elements.Add(elementDetails);
                    }

                    return Response.Success($"Detailed inspection of array '{propertyName}' with {list.Count} elements", new
                    {
                        propertyName = propertyName,
                        count = list.Count,
                        elementType = elementType?.Name ?? "unknown",
                        elementFullType = elementType?.FullName ?? "unknown", 
                        elements = elements
                    });
                }
                else
                {
                    return Response.Error($"Property '{propertyName}' is not an array or list");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error inspecting array '{propertyName}': {e.Message}");
            }
        }
        private static object AddArrayElement(ScriptableObject asset, JObject @params)
        {
            string propertyName = @params["property_name"]?.ToString();
            var propertyValue = @params["property_value"];
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("property_name parameter is required for add_array_element action.");
            }
            
            if (propertyValue == null)
            {
                return Response.Error("property_value parameter is required for add_array_element action.");
            }

            try
            {
                var field = GetField(asset.GetType(), propertyName);
                if (field == null)
                {
                    return Response.Error($"Property '{propertyName}' not found on {asset.GetType().Name}");
                }

                var arrayValue = field.GetValue(asset);
                
                if (arrayValue is IList list)
                {
                    // Get the element type for generic lists
                    Type elementType = null;
                    if (field.FieldType.IsGenericType)
                    {
                        elementType = field.FieldType.GetGenericArguments()[0];
                    }
                    else if (field.FieldType.IsArray)
                    {
                        elementType = field.FieldType.GetElementType();
                    }

                    if (elementType != null)
                    {
                        var convertedValue = ConvertValue(propertyValue, elementType, null);
                        list.Add(convertedValue);
                        
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssets();

                        return Response.Success($"Added element to array '{propertyName}'", new
                        {
                            propertyName = propertyName,
                            newIndex = list.Count - 1,
                            addedValue = ConvertValueToString(convertedValue),
                            newCount = list.Count
                        });
                    }
                    else
                    {
                        return Response.Error($"Could not determine element type for array '{propertyName}'");
                    }
                }
                else
                {
                    return Response.Error($"Property '{propertyName}' is not an array or list");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error adding array element to '{propertyName}': {e.Message}");
            }
        }
        private static object RemoveArrayElement(ScriptableObject asset, JObject @params)
        {
            string propertyName = @params["property_name"]?.ToString();
            int? index = @params["index"]?.ToObject<int>();
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("property_name parameter is required for remove_array_element action.");
            }
            
            if (!index.HasValue)
            {
                return Response.Error("index parameter is required for remove_array_element action.");
            }

            try
            {
                var field = GetField(asset.GetType(), propertyName);
                if (field == null)
                {
                    return Response.Error($"Property '{propertyName}' not found on {asset.GetType().Name}");
                }

                var arrayValue = field.GetValue(asset);
                
                if (arrayValue is IList list)
                {
                    if (index.Value < 0 || index.Value >= list.Count)
                    {
                        return Response.Error($"Index {index.Value} is out of range for array '{propertyName}' (count: {list.Count})");
                    }

                    var removedValue = list[index.Value];
                    list.RemoveAt(index.Value);
                    
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();

                    return Response.Success($"Removed element at index {index.Value} from array '{propertyName}'", new
                    {
                        propertyName = propertyName,
                        removedIndex = index.Value,
                        removedValue = ConvertValueToString(removedValue),
                        newCount = list.Count
                    });
                }
                else
                {
                    return Response.Error($"Property '{propertyName}' is not an array or list");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error removing array element from '{propertyName}': {e.Message}");
            }
        }
        private static object SetArrayElement(ScriptableObject asset, JObject @params)
        {
            string propertyName = @params["property_name"]?.ToString();
            var propertyValue = @params["property_value"];
            int? index = @params["index"]?.ToObject<int>();
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("property_name parameter is required for set_array_element action.");
            }
            
            if (propertyValue == null)
            {
                return Response.Error("property_value parameter is required for set_array_element action.");
            }
            
            if (!index.HasValue)
            {
                return Response.Error("index parameter is required for set_array_element action.");
            }

            try
            {
                var field = GetField(asset.GetType(), propertyName);
                if (field == null)
                {
                    return Response.Error($"Property '{propertyName}' not found on {asset.GetType().Name}");
                }

                var arrayValue = field.GetValue(asset);
                
                if (arrayValue is IList list)
                {
                    if (index.Value < 0 || index.Value >= list.Count)
                    {
                        return Response.Error($"Index {index.Value} is out of range for array '{propertyName}' (count: {list.Count})");
                    }

                    // Get the element type
                    Type elementType = null;
                    if (field.FieldType.IsGenericType)
                    {
                        elementType = field.FieldType.GetGenericArguments()[0];
                    }
                    else if (field.FieldType.IsArray)
                    {
                        elementType = field.FieldType.GetElementType();
                    }

                    if (elementType != null)
                    {
                        var oldValue = list[index.Value];
                        var convertedValue = ConvertValue(propertyValue, elementType, null);
                        list[index.Value] = convertedValue;
                        
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssets();

                        return Response.Success($"Set element at index {index.Value} in array '{propertyName}'", new
                        {
                            propertyName = propertyName,
                            index = index.Value,
                            oldValue = ConvertValueToString(oldValue),
                            newValue = ConvertValueToString(convertedValue)
                        });
                    }
                    else
                    {
                        return Response.Error($"Could not determine element type for array '{propertyName}'");
                    }
                }
                else
                {
                    return Response.Error($"Property '{propertyName}' is not an array or list");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error setting array element in '{propertyName}': {e.Message}");
            }
        }
        // Helper methods
        private static FieldInfo GetField(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static string ConvertValueToString(object value)
        {
            if (value == null) return "null";
            
            if (value is string) return $"\"{value}\"";
            if (value is bool) return value.ToString().ToLower();
            if (value is Vector2 v2) return $"({v2.x}, {v2.y})";
            if (value is Vector3 v3) return $"({v3.x}, {v3.y}, {v3.z})";
            if (value is Vector4 v4) return $"({v4.x}, {v4.y}, {v4.z}, {v4.w})";
            if (value is Color color) return $"RGBA({color.r}, {color.g}, {color.b}, {color.a})";
            if (value is IList list)
            {
                // Enhanced array display for better inspection
                if (list.Count == 0)
                {
                    return "Array[0] (empty)";
                }
                
                // For voice arrays and other complex objects, show more detail
                var elementType = list.GetType().GetGenericArguments().FirstOrDefault()?.Name ?? "object";
                return $"Array[{list.Count}] of {elementType}";
            }
            if (value is UnityEngine.Object unityObj)
            {
                return unityObj ? unityObj.name : "null";
            }
            
            return value.ToString();
        }

        private static object InspectObjectProperties(object obj)
        {
            if (obj == null) return null;

            try
            {
                var type = obj.GetType();
                var properties = new Dictionary<string, object>();

                // Get all public fields and properties
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(obj);
                        properties[field.Name] = ConvertValueForInspection(value);
                    }
                    catch (Exception e)
                    {
                        properties[field.Name] = $"Error: {e.Message}";
                    }
                }

                foreach (var prop in props)
                {
                    try
                    {
                        var value = prop.GetValue(obj);
                        properties[prop.Name] = ConvertValueForInspection(value);
                    }
                    catch (Exception e)
                    {
                        properties[prop.Name] = $"Error: {e.Message}";
                    }
                }

                return properties;
            }
            catch (Exception e)
            {
                return $"Inspection error: {e.Message}";
            }
        }
        private static object ConvertValueForInspection(object value)
        {
            if (value == null) return null;
            
            // For primitive types, return as-is
            if (value.GetType().IsPrimitive || value is string || value is decimal)
            {
                return value;
            }
            
            // For Unity types, convert to readable format
            if (value is Vector2 v2) return new { x = v2.x, y = v2.y };
            if (value is Vector3 v3) return new { x = v3.x, y = v3.y, z = v3.z };
            if (value is Vector4 v4) return new { x = v4.x, y = v4.y, z = v4.z, w = v4.w };
            if (value is Color color) return new { r = color.r, g = color.g, b = color.b, a = color.a };
            
            // For Unity Objects, return name and type
            if (value is UnityEngine.Object unityObj)
            {
                return new 
                { 
                    name = unityObj ? unityObj.name : "null",
                    type = unityObj?.GetType()?.Name ?? "null",
                    instanceId = unityObj ? unityObj.GetInstanceID() : 0
                };
            }
            
            // For enums, return string representation
            if (value.GetType().IsEnum)
            {
                return value.ToString();
            }
            
            // For arrays/lists, return summary
            if (value is IList list)
            {
                return $"Array[{list.Count}] of {value.GetType().GetGenericArguments().FirstOrDefault()?.Name ?? "object"}";
            }
            
            // For other complex objects, return type info
            return new 
            { 
                type = value.GetType().Name,
                toString = value.ToString()
            };
        }
        private static object ConvertValue(JToken token, Type targetType, string typeHint)
        {
            try
            {
                // Handle null values
                if (token == null || token.Type == JTokenType.Null)
                {
                    return null;
                }

                // Handle basic types
                if (targetType == typeof(string))
                {
                    return token.ToString();
                }
                
                if (targetType == typeof(int))
                {
                    return token.ToObject<int>();
                }
                
                if (targetType == typeof(float))
                {
                    return token.ToObject<float>();
                }
                
                if (targetType == typeof(double))
                {
                    return token.ToObject<double>();
                }
                
                if (targetType == typeof(bool))
                {
                    return token.ToObject<bool>();
                }

                // Handle Unity types
                if (targetType == typeof(Vector2))
                {
                    if (token.Type == JTokenType.Array)
                    {
                        var array = token.ToObject<float[]>();
                        return new Vector2(array[0], array[1]);
                    }
                    else if (token.Type == JTokenType.Object)
                    {
                        return token.ToObject<Vector2>();
                    }
                }
                
                if (targetType == typeof(Vector3))
                {
                    if (token.Type == JTokenType.Array)
                    {
                        var array = token.ToObject<float[]>();
                        return new Vector3(array[0], array[1], array[2]);
                    }
                    else if (token.Type == JTokenType.Object)
                    {
                        return token.ToObject<Vector3>();
                    }
                }
                
                if (targetType == typeof(Vector4))
                {
                    if (token.Type == JTokenType.Array)
                    {
                        var array = token.ToObject<float[]>();
                        return new Vector4(array[0], array[1], array[2], array[3]);
                    }
                    else if (token.Type == JTokenType.Object)
                    {
                        return token.ToObject<Vector4>();
                    }
                }
                
                if (targetType == typeof(Color))
                {
                    if (token.Type == JTokenType.Array)
                    {
                        var array = token.ToObject<float[]>();
                        return new Color(array[0], array[1], array[2], array.Length > 3 ? array[3] : 1f);
                    }
                    else if (token.Type == JTokenType.Object)
                    {
                        return token.ToObject<Color>();
                    }
                }

                // Handle enums
                if (targetType.IsEnum)
                {
                    if (token.Type == JTokenType.String)
                    {
                        return Enum.Parse(targetType, token.ToString());
                    }
                    else if (token.Type == JTokenType.Integer)
                    {
                        return Enum.ToObject(targetType, token.ToObject<int>());
                    }
                }

                // Handle Unity Object references by name or path
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    string assetReference = token.ToString();
                    return AssetDatabase.LoadAssetAtPath(assetReference, targetType);
                }

                // Fallback: try direct conversion
                return token.ToObject(targetType);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to convert value '{token}' to type {targetType.Name}: {e.Message}");
            }
        }
    }
}