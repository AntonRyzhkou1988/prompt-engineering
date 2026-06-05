using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PromptEngineering.SpaceMissions;
using SpaceMissions.McpServer.Tools;

const string DatasetPathEnv = "SPACE_MISSIONS_DATASET_PATH";
const string DefaultDatasetPath = "dataset/space_missions.csv";

var datasetPath = Environment.GetEnvironmentVariable(DatasetPathEnv);
if (string.IsNullOrWhiteSpace(datasetPath))
    datasetPath = DefaultDatasetPath;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<ISpaceMissionQueryService>(_ => new SpaceMissionQueryService(datasetPath));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<SpaceMissionTools>();

await builder.Build().RunAsync();
