#!/bin/bash

# Test NHS MCP Azure Functions
# Usage: ./test-functions.sh [base-url]
# Example: ./test-functions.sh http://localhost:7071
# Example: ./test-functions.sh https://nhsmcp-functions.azurewebsites.net

BASE_URL=${1:-"http://localhost:7071"}

echo "=================================================="
echo "Testing NHS MCP Azure Functions"
echo "Base URL: $BASE_URL"
echo "=================================================="
echo ""

# Test 1: List Tools
echo "Test 1: List Tools"
echo "GET $BASE_URL/mcp/tools"
curl -s "$BASE_URL/mcp/tools" | jq '.' || echo "Failed"
echo ""
echo "=================================================="
echo ""

# Test 2: Get Organisation Types (SSE)
echo "Test 2: Get Organisation Types (SSE)"
echo "GET $BASE_URL/mcp/tools/get_organisation_types"
timeout 5 curl -N "$BASE_URL/mcp/tools/get_organisation_types" 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 3: Convert Postcode (GET)
echo "Test 3: Convert Postcode to Coordinates (GET)"
echo "GET $BASE_URL/mcp/tools/convert_postcode_to_coordinates?postcode=SW1A%201AA"
timeout 5 curl -N "$BASE_URL/mcp/tools/convert_postcode_to_coordinates?postcode=SW1A%201AA" 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 4: Convert Postcode (POST)
echo "Test 4: Convert Postcode to Coordinates (POST)"
echo "POST $BASE_URL/mcp/tools/convert_postcode_to_coordinates"
timeout 5 curl -N -X POST "$BASE_URL/mcp/tools/convert_postcode_to_coordinates" \
  -H "Content-Type: application/json" \
  -d '{"postcode":"M1 1AE"}' 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 5: Search by Postcode (GET)
echo "Test 5: Search Organisations by Postcode (GET)"
echo "GET $BASE_URL/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A%201AA&maxResults=3"
timeout 10 curl -N "$BASE_URL/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A%201AA&maxResults=3" 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 6: Search by Postcode (POST)
echo "Test 6: Search Organisations by Postcode (POST)"
echo "POST $BASE_URL/mcp/tools/search_organisations_by_postcode"
timeout 10 curl -N -X POST "$BASE_URL/mcp/tools/search_organisations_by_postcode" \
  -H "Content-Type: application/json" \
  -d '{"organisationType":"GPB","postcode":"M1 1AE","maxResults":3}' 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 7: Search by Coordinates (GET)
echo "Test 7: Search Organisations by Coordinates (GET)"
echo "GET $BASE_URL/mcp/tools/search_organisations_by_coordinates?organisationType=HOS&latitude=51.5074&longitude=-0.1278&maxResults=3"
timeout 10 curl -N "$BASE_URL/mcp/tools/search_organisations_by_coordinates?organisationType=HOS&latitude=51.5074&longitude=-0.1278&maxResults=3" 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 8: Search by Coordinates (POST)
echo "Test 8: Search Organisations by Coordinates (POST)"
echo "POST $BASE_URL/mcp/tools/search_organisations_by_coordinates"
timeout 10 curl -N -X POST "$BASE_URL/mcp/tools/search_organisations_by_coordinates" \
  -H "Content-Type: application/json" \
  -d '{"organisationType":"DEN","latitude":53.4808,"longitude":-2.2426,"maxResults":3}' 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 9: Get Health Topic (GET)
echo "Test 9: Get Health Topic (GET)"
echo "GET $BASE_URL/mcp/tools/get_health_topic?topic=asthma"
timeout 10 curl -N "$BASE_URL/mcp/tools/get_health_topic?topic=asthma" 2>/dev/null
echo ""
echo "=================================================="
echo ""

# Test 10: Get Health Topic (POST)
echo "Test 10: Get Health Topic (POST)"
echo "POST $BASE_URL/mcp/tools/get_health_topic"
timeout 10 curl -N -X POST "$BASE_URL/mcp/tools/get_health_topic" \
  -H "Content-Type: application/json" \
  -d '{"topic":"diabetes"}' 2>/dev/null
echo ""
echo "=================================================="
echo ""

echo "All tests completed!"
echo ""
