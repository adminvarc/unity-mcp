"""
Defines the create_scriptable_object tool for creating ScriptableObjects in Unity.
"""
import asyncio
from typing import Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_create_scriptable_object_tools(mcp: FastMCP):
    """Registers the create_scriptable_object tool with the MCP server."""

    @mcp.tool()
    async def create_scriptable_object(
        ctx: Context,
        action: str,
        name: str = None,
        subfolder: str = None,
        type_name: str = None,
        path: str = None
    ) -> Dict[str, Any]:
        """Creates ScriptableObjects in Unity project, specifically designed for AICharacter and AIInteraction objects.

        Args:
            ctx: The MCP context.
            action: The action to perform (create_ai_character, create_ai_interaction, create_custom, list_types).
            name: Name for the ScriptableObject (required for create actions).
            subfolder: Optional subfolder within the type's base directory (e.g., 'AD', 'CL', 'DITO').
            type_name: Custom ScriptableObject type name (required for create_custom action).
            path: Full path for custom ScriptableObject creation (required for create_custom action).

        Returns:
            A dictionary with operation results ('success', 'message', 'data').
        """
        # Prepare parameters for the C# handler
        params_dict = {
            "action": action.lower(),
        }
        
        # Add optional parameters if provided
        if name is not None:
            params_dict["name"] = name
        if subfolder is not None:
            params_dict["subfolder"] = subfolder
        if type_name is not None:
            params_dict["type_name"] = type_name
        if path is not None:
            params_dict["path"] = path

        # Get Unity connection and execute the command
        connection = get_unity_connection()
        result = await asyncio.to_thread(
            connection.send_command, 
            "HandleCreateScriptableObject", 
            params_dict
        )
        
        return result
