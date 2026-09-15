using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Edam.AgentFramework.Core;
using Edam.Mcp.Server;
using Edam.Services.Contracts;
using Edam.Services.Core;

// BL-6.3: MCP shell exposing the Wave-1 boundaries to MCP clients over stdio (AI-integration
// and scripting avenue). D5: the Wave-1 descriptor is what the MCP layer consumes, so deeper
// operations (BL-6.6 persistence, etc.) swap in transparently behind the same interfaces.
var config = new ProviderConfig();
var host = KernelHost.PrepareKernelHost(config);
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var registry = new ToolRegistry(json, host);

var svcs = new IWave1Service[]
{
    new InMemoryCatalogService(NullLogger<InMemoryCatalogService>.Instance),
    new InMemoryBookletMappingService(NullLogger<InMemoryBookletMappingService>.Instance),
    new InMemoryVocabularyService(NullLogger<InMemoryVocabularyService>.Instance),
};

registry.AddTool(new McpTool(
    "edam_wave1",
    "Enumerate Wave-1 onboarded services with their lifecycle/health descriptors.",
    JsonDocument.Parse("""{"type":"object","properties":{},"additionalProperties":false}"""),
    (args, ct) => Task.FromResult(RequestResult.Okey(svcs.Select(s => s.Describe()).ToList()))));

var server = new McpServer(host, json, registry);
await server.RunStdIoAsync(CancellationToken.None);
