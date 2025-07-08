using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Helpers; // For Response class

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles ScriptableObject creation operations within the Unity project.
    /// </summary>
    public static class CreateScriptableObject
    {
        /// <summary>
        /// Main handler for ScriptableObject creation commands
        /// </summary>
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("Action parameter is required.");
            }

            try
            {
                switch (action)
                {
                    case "create_ai_character":
                        return CreateAICharacter(@params);
                    
                    case "create_ai_interaction":
                        return CreateAIInteraction(@params);
                    
                    case "create_custom":
                        return CreateCustomScriptableObject(@params);
                    
                    case "list_types":
                        return ListAvailableTypes();
                    
                    default:
                        return Response.Error($"Unknown action: {action}. Available actions: create_ai_character, create_ai_interaction, create_custom, list_types");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CreateScriptableObject] Action '{action}' failed: {e}");
                return Response.Error($"Error handling command: {e.Message}");
            }
        }

        private static object CreateAICharacter(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string subfolder = @params["subfolder"]?.ToString() ?? "";
            
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Parameter 'name' is required for creating AI Character");
            }

            var result = ScriptableObjectUtility.CreateAICharacter(name, subfolder);
            
            if (result.success)
            {
                return Response.Success(result.message, new
                {
                    assetPath = result.assetPath,
                    type = "AICharacter",
                    name = name,
                    subfolder = subfolder
                });
            }
            else
            {
                return Response.Error(result.message);
            }
        }

        private static object CreateAIInteraction(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string subfolder = @params["subfolder"]?.ToString() ?? "";
            
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Parameter 'name' is required for creating AI Interaction");
            }

            var result = ScriptableObjectUtility.CreateAIInteraction(name, subfolder);
            
            if (result.success)
            {
                return Response.Success(result.message, new
                {
                    assetPath = result.assetPath,
                    type = "AIInteraction",
                    name = name,
                    subfolder = subfolder
                });
            }
            else
            {
                return Response.Error(result.message);
            }
        }

        private static object CreateCustomScriptableObject(JObject @params)
        {
            string typeName = @params["type_name"]?.ToString();
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString();
            
            if (string.IsNullOrEmpty(typeName))
            {
                return Response.Error("Parameter 'type_name' is required for creating custom ScriptableObject");
            }
            
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Parameter 'name' is required for creating custom ScriptableObject");
            }
            
            if (string.IsNullOrEmpty(path))
            {
                return Response.Error("Parameter 'path' is required for creating custom ScriptableObject");
            }

            var result = ScriptableObjectUtility.CreateScriptableObject(typeName, path, name);
            
            if (result.success)
            {
                return Response.Success(result.message, new
                {
                    assetPath = result.assetPath,
                    type = typeName,
                    name = name,
                    path = path
                });
            }
            else
            {
                return Response.Error(result.message);
            }
        }

        private static object ListAvailableTypes()
        {
            try
            {
                string[] types = ScriptableObjectUtility.GetAvailableScriptableObjectTypes();
                
                return Response.Success($"Found {types.Length} ScriptableObject types", new
                {
                    types = types
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Error listing types: {e.Message}");
            }
        }
    }
}