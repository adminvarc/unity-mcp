"""
Defines the modify_scriptable_object tool for reading and modifying ScriptableObject properties in Unity.
"""
import asyncio
from typing import Dict, Any, Optional, Union
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_modify_scriptable_object_tools(mcp: FastMCP):
    """Registers the modify_scriptable_object tool with the MCP server."""

    @mcp.tool()
    async def modify_scriptable_object(
        ctx: Context,
        action: str,
        asset_path: str,
        property_name: Optional[str] = None,
        property_value: Optional[Union[str, int, float, bool, list, dict]] = None,
        property_type: Optional[str] = None,
        index: Optional[int] = None
    ) -> Dict[str, Any]:
        """Manages ScriptableObject properties: read, write, and inspect ScriptableObjects.

        Args:
            ctx: The MCP context.
            action: Operation to perform:
                - 'get_properties': List all properties and their current values
                - 'get_property': Get value of a specific property
                - 'set_property': Set value of a specific property
                - 'get_info': Get basic info about the ScriptableObject
                - 'list_array_elements': List elements in an array/list property
                - 'inspect_array_detailed': Get detailed inspection of array elements (especially useful for voice attributes)
                - 'add_array_element': Add an element to an array/list property
                - 'remove_array_element': Remove an element from an array/list property
                - 'set_array_element': Set value of a specific array element
            asset_path: Path to the ScriptableObject asset (e.g., "Assets/Data/MyCharacter.asset")
            property_name: Name of the property to get/set (required for property operations)
            property_value: New value for the property (required for set operations)
            property_type: Type hint for property conversion (optional, auto-detected if not provided)
            index: Array index for array operations (required for array element operations)

        Returns:
            A dictionary with operation results ('success', 'message', 'data').
        """
        # Validate required parameters
        if not asset_path:
            return {"success": False, "message": "asset_path is required", "data": None}
        
        property_actions = ['get_property', 'set_property', 'list_array_elements', 'inspect_array_detailed', 'add_array_element', 'remove_array_element', 'set_array_element']
        if action in property_actions and not property_name:
            return {"success": False, "message": f"property_name is required for action '{action}'", "data": None}
        
        set_actions = ['set_property', 'add_array_element', 'set_array_element']
        if action in set_actions and property_value is None:
            return {"success": False, "message": f"property_value is required for action '{action}'", "data": None}
        
        array_element_actions = ['remove_array_element', 'set_array_element']
        if action in array_element_actions and index is None:
            return {"success": False, "message": f"index is required for action '{action}'", "data": None}

        # Prepare parameters for the C# handler
        params_dict = {
            "action": action.lower(),
            "asset_path": asset_path
        }
        
        # Add optional parameters if provided
        if property_name is not None:
            params_dict["property_name"] = property_name
        if property_value is not None:
            params_dict["property_value"] = property_value
        if property_type is not None:
            params_dict["property_type"] = property_type
        if index is not None:
            params_dict["index"] = index

        # Get Unity connection and execute the command
        connection = get_unity_connection()
        result = await asyncio.to_thread(
            connection.send_command, 
            "HandleModifyScriptableObject", 
            params_dict
        )
        
        return result
