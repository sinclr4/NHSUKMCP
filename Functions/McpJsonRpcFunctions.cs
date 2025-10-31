using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using NHSUKMCP.Tools;

namespace NHSUKMCP.Functions;

/// <summary>
/// Azure Functions that provide MCP protocol over HTTP with JSON-RPC
/// Based on MCP specification: https://spec.modelcontextprotocol.io/
/// </summary>
public class McpJsonRpcFunctions
{
    private readonly ILogger<McpJsonRpcFunctions> _logger;
    private readonly NHSOrganisationSearchTools _orgTools;
 private readonly NHSHealthContentTools _healthTools;

    public McpJsonRpcFunctions(
    ILogger<McpJsonRpcFunctions> logger,
        NHSOrganisationSearchTools orgTools,
        NHSHealthContentTools healthTools)
    {
    _logger = logger;
        _orgTools = orgTools;
      _healthTools = healthTools;
    }

    /// <summary>
    /// Main MCP JSON-RPC endpoint
    /// </summary>
  [Function("Mcp")]
    public async Task<HttpResponseData> Mcp(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp")] HttpRequestData req)
    {
        _logger.LogInformation("MCP JSON-RPC request received");

        try
        {
   var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
  var jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(requestBody);

            if (jsonRpcRequest == null)
      {
            return CreateJsonRpcError(req, null, -32700, "Parse error");
            }

   object? result = jsonRpcRequest.Method switch
   {
           "initialize" => HandleInitialize(jsonRpcRequest),
       "tools/list" => HandleToolsList(),
        "tools/call" => await HandleToolsCallAsync(jsonRpcRequest),
       "ping" => new { },
     _ => throw new Exception($"Method not found: {jsonRpcRequest.Method}")
            };

            return CreateJsonRpcResponse(req, jsonRpcRequest.Id, result);
        }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error processing MCP request");
return CreateJsonRpcError(req, null, -32603, $"Internal error: {ex.Message}");
        }
    }

    private object HandleInitialize(JsonRpcRequest request)
    {
   return new InitializeResult
        {
ProtocolVersion = "2024-11-05",
ServerInfo = new Implementation
     {
      Name = "nhs-uk-mcp-server",
          Version = "1.0.0"
            },
            Capabilities = new ServerCapabilities
 {
          Tools = new ToolsCapability { ListChanged = true }
        }
   };
    }

    private object HandleToolsList()
    {
        var tools = new List<object>
  {
            new
            {
      name = "get_organisation_types",
        description = "Get a list of all available NHS organisation types",
      inputSchema = new
    {
  type = "object",
                  properties = new { },
       required = new string[] { }
         }
   },
      new
  {
     name = "convert_postcode_to_coordinates",
  description = "Convert a UK postcode to latitude and longitude coordinates",
   inputSchema = new
       {
      type = "object",
 properties = new
          {
  postcode = new { type = "string", description = "UK postcode" }
  },
   required = new[] { "postcode" }
 }
   },
 new
       {
name = "search_organisations_by_postcode",
    description = "Search for NHS organisations near a postcode",
      inputSchema = new
      {
     type = "object",
   properties = new
             {
  organisationType = new { type = "string", description = "Organisation type" },
   postcode = new { type = "string", description = "UK postcode" },
    maxResults = new { type = "integer", description = "Max results (default: 10)" }
           },
    required = new[] { "organisationType", "postcode" }
      }
         },
     new
 {
     name = "search_organisations_by_coordinates",
     description = "Search for NHS organisations near coordinates",
   inputSchema = new
   {
    type = "object",
     properties = new
    {
 organisationType = new { type = "string" },
    latitude = new { type = "number" },
       longitude = new { type = "number" },
maxResults = new { type = "integer" }
    },
     required = new[] { "organisationType", "latitude", "longitude" }
      }
         },
   new
            {
  name = "get_health_topic",
          description = "Get NHS health topic information",
inputSchema = new
    {
  type = "object",
     properties = new
        {
         topic = new { type = "string", description = "Health topic slug" }
  },
  required = new[] { "topic" }
          }
 }
        };

   return new { tools };
    }

 private async Task<object> HandleToolsCallAsync(JsonRpcRequest request)
    {
        var paramsJson = JsonSerializer.Serialize(request.Params);
  var callParams = JsonSerializer.Deserialize<ToolCallParams>(paramsJson);

 if (callParams == null)
      throw new Exception("Invalid tool call parameters");

        try
    {
            var args = callParams.Arguments ?? new Dictionary<string, JsonElement>();
            
    object? toolResult = callParams.Name switch
    {
       "get_organisation_types" => await _orgTools.GetOrganisationTypesAsync(),
          
      "convert_postcode_to_coordinates" => await _orgTools.ConvertPostcodeToCoordinatesAsync(
        args["postcode"].GetString()!),
     
  "search_organisations_by_postcode" => await _orgTools.SearchOrganisationsByPostcodeAsync(
            args["organisationType"].GetString()!,
      args["postcode"].GetString()!,
      args.TryGetValue("maxResults", out var mr) ? mr.GetInt32() : 10),

            "search_organisations_by_coordinates" => await _orgTools.SearchOrganisationsByCoordinatesAsync(
        args["organisationType"].GetString()!,
  args["latitude"].GetDouble(),
 args["longitude"].GetDouble(),
    args.TryGetValue("maxResults", out var mr2) ? mr2.GetInt32() : 10),
              
                "get_health_topic" => await _healthTools.GetHealthTopicAsync(
   args["topic"].GetString()!),
           
             _ => throw new Exception($"Unknown tool: {callParams.Name}")
          };

     return new
  {
    content = new[]
         {
    new { type = "text", text = JsonSerializer.Serialize(toolResult) }
  }
       };
        }
        catch (Exception ex)
        {
       _logger.LogError(ex, "Error calling tool {ToolName}", callParams.Name);
       return new
     {
     content = new[]
           {
             new { type = "text", text = $"Error: {ex.Message}" }
      },
                isError = true
            };
        }
}

    private HttpResponseData CreateJsonRpcResponse(HttpRequestData req, object? id, object? result)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        
        var jsonRpcResponse = new
        {
            jsonrpc = "2.0",
            id = id,
result = result
        };
        
        response.WriteString(JsonSerializer.Serialize(jsonRpcResponse));
        return response;
    }

    private HttpResponseData CreateJsonRpcError(HttpRequestData req, object? id, int code, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
      
   var jsonRpcError = new
        {
            jsonrpc = "2.0",
            id = id,
            error = new
            {
      code = code,
         message = message
    }
        };
        
        response.WriteString(JsonSerializer.Serialize(jsonRpcError));
        return response;
    }
}

// Request/Response classes
public class JsonRpcRequest
{
    public string JsonRpc { get; set; } = "2.0";
    public object? Id { get; set; }
    public string Method { get; set; } = "";
    public object? Params { get; set; }
}

public class ToolCallParams
{
    public string Name { get; set; } = "";
    public Dictionary<string, JsonElement>? Arguments { get; set; }
}
