using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Utility class for creating and managing ScriptableObjects in Unity.
    /// Provides reusable functionality for ScriptableObject creation and type discovery.
    /// Can be used independently of the MCP system.
    /// </summary>
    public static class ScriptableObjectUtility
    {
        /// <summary>
        /// Creates a ScriptableObject of the specified type at the given path
        /// </summary>
        /// <param name="typeName">The full type name of the ScriptableObject (e.g., "AICharacter")</param>
        /// <param name="assetPath">The path where to create the asset (e.g., "Assets/_wondder_AI/AICharacters/")</param>
        /// <param name="fileName">The name for the new asset file (without .asset extension)</param>
        /// <returns>Success status and path of created asset</returns>
        public static (bool success, string message, string assetPath) CreateScriptableObject(
            string typeName, 
            string assetPath, 
            string fileName)
        {
            try
            {
                // Find the ScriptableObject type
                Type scriptableObjectType = FindScriptableObjectType(typeName);
                if (scriptableObjectType == null)
                {
                    return (false, $"ScriptableObject type '{typeName}' not found", null);
                }

                // Create the ScriptableObject instance
                ScriptableObject asset = ScriptableObject.CreateInstance(scriptableObjectType);
                if (asset == null)
                {
                    return (false, $"Failed to create instance of '{typeName}'", null);
                }

                // Ensure the directory exists
                string directoryPath = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Create the full asset path
                string fullAssetPath = Path.Combine(assetPath, fileName + ".asset").Replace("\\", "/");
                
                // Make sure we don't overwrite existing assets
                fullAssetPath = AssetDatabase.GenerateUniqueAssetPath(fullAssetPath);

                // Create the asset
                AssetDatabase.CreateAsset(asset, fullAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Select the created asset in the Project window
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);

                return (true, $"Successfully created {typeName} at {fullAssetPath}", fullAssetPath);
            }
            catch (Exception e)
            {
                return (false, $"Error creating ScriptableObject: {e.Message}", null);
            }
        }

        /// <summary>
        /// Creates an AICharacter ScriptableObject
        /// </summary>
        public static (bool success, string message, string assetPath) CreateAICharacter(
            string characterName, 
            string subfolder = "")
        {
            string basePath = "Assets/_wondder_AI/AICharacters/";
            if (!string.IsNullOrEmpty(subfolder))
            {
                // Ensure subfolder is in capitals
                basePath = Path.Combine(basePath, subfolder.ToUpper()).Replace("\\", "/") + "/";
            }

            return CreateScriptableObject("AICharacter", basePath, characterName);
        }

        /// <summary>
        /// Creates an AIInteraction ScriptableObject
        /// </summary>
        public static (bool success, string message, string assetPath) CreateAIInteraction(
            string interactionName, 
            string subfolder = "")
        {
            string basePath = "Assets/_wondder_AI/AIInteractions/";
            if (!string.IsNullOrEmpty(subfolder))
            {
                // Ensure subfolder is in capitals
                basePath = Path.Combine(basePath, subfolder.ToUpper()).Replace("\\", "/") + "/";
            }

            return CreateScriptableObject("AIInteraction", basePath, interactionName);
        }

        /// <summary>
        /// Finds a ScriptableObject type by name
        /// </summary>
        private static Type FindScriptableObjectType(string typeName)
        {
            // First try to find in the current assembly
            Type type = Type.GetType(typeName);
            if (type != null && type.IsSubclassOf(typeof(ScriptableObject)))
            {
                return type;
            }

            // Search through all loaded assemblies
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName);
                    if (type != null && type.IsSubclassOf(typeof(ScriptableObject)))
                    {
                        return type;
                    }

                    // Also try without namespace
                    foreach (var t in assembly.GetTypes())
                    {
                        if (t.Name == typeName && t.IsSubclassOf(typeof(ScriptableObject)))
                        {
                            return t;
                        }
                    }
                }
                catch
                {
                    // Ignore assemblies that can't be reflected
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Lists all available ScriptableObject types in the project
        /// </summary>
        public static string[] GetAvailableScriptableObjectTypes()
        {
            var types = new System.Collections.Generic.List<string>();

            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(ScriptableObject)) && 
                            !type.IsAbstract && 
                            type.IsPublic)
                        {
                            types.Add(type.Name);
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return types.ToArray();
        }
    }
}