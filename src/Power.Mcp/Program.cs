using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Power.Agent;
using Power.Mcp;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton<AgentWorkspace>();
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<PowerTools>(AgentReply.JsonOptions);
await builder.Build().RunAsync();
