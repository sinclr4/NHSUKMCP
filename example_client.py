#!/usr/bin/env python3
"""
Example MCP Client for NHS Organizations Server

Install dependencies:
    pip install mcp

Run:
    python example_client.py
"""

import asyncio
import os
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def main():
    # Configure server parameters
    server_params = StdioServerParameters(
        command="/Users/robsinclair/NHSOrgsMCP/bin/Release/net9.0/NHSOrgsMCP",
        env={
            "AZURE_SEARCH_ENDPOINT": "https://nhsuksearchintuks.search.windows.net",
            "AZURE_SEARCH_API_KEY": "your-key-here",
            "AZURE_SEARCH_POSTCODE_INDEX": "postcodesandplaces-1-0-b-int",
            "AZURE_SEARCH_SERVICE_INDEX": "service-search-internal-3-11"
        }
    )

    # Connect to server
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            # Initialize connection
            await session.initialize()
            print("Connected to NHS Organizations MCP Server")

            # List available tools
            tools = await session.list_tools()
            print("\nAvailable tools:")
            for tool in tools.tools:
                print(f"- {tool.name}: {tool.description}")

            # Example: Get organization types
            print("\n--- Getting Organization Types ---")
            result = await session.call_tool("get_organization_types", arguments={})
            print(result.content[0].text)

            # Example: Convert postcode
            print("\n--- Converting Postcode SW1A 1AA ---")
            result = await session.call_tool(
                "convert_postcode_to_coordinates",
                arguments={"postcode": "SW1A 1AA"}
            )
            print(result.content[0].text)

            # Example: Search pharmacies
            print("\n--- Searching Pharmacies near SW1A 1AA ---")
            result = await session.call_tool(
                "search_organizations_by_postcode",
                arguments={
                    "organizationType": "PHA",
                    "postcode": "SW1A 1AA",
                    "maxResults": 3
                }
            )
            print(result.content[0].text)

            print("\nDisconnected")

if __name__ == "__main__":
    asyncio.run(main())
