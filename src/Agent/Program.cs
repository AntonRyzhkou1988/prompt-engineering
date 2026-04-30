using System.Reflection;
using Agent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PromptEngineering.LLM.Configurations;

var settings = new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory,
    Args = args,
};
var builder = Host.CreateApplicationBuilder(settings);

// Overrides values from appsettings.json; uses UserSecretsId from Agent.csproj (dotnet user-secrets set).
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddGenAi(builder.Configuration);

builder.Services
    .AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.SectionName));

builder.Services.AddSingleton<ToolDomainMapper>();
builder.Services.AddSingleton<IWeatherAgentService, OpenMeteoWeatherAgentService>();
builder.Services.AddSingleton<INewsAgentService, DuckDuckGoNewsAgentService>();
builder.Services.AddScoped<WeatherNewsAgentService>();

var app = builder.Build();

var question = args.Length > 0 ? string.Join(" ", args) : null;
if (string.IsNullOrWhiteSpace(question))
{
    Console.WriteLine("Usage: Agent [question...]");
    Console.WriteLine("Example: Agent \"What is the weather and the latest news in Paris?\"");
    Console.Write("Question: ");
    question = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(question))
{
    Console.WriteLine("No question provided; exiting.");
    return;
}

await using var scope = app.Services.CreateAsyncScope();
var agent = scope.ServiceProvider.GetRequiredService<WeatherNewsAgentService>();
var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Agent");
try
{
    var result = await agent.RunAsync(question).ConfigureAwait(false);
    Console.WriteLine();
    Console.WriteLine("--- Answer ---");
    Console.WriteLine(result.AnswerText);
    Console.WriteLine();
    Console.WriteLine("--- Tools invoked ---");
    Console.WriteLine(result.ToolNamesInvoked.Count > 0 ? string.Join(", ", result.ToolNamesInvoked) : "(none)");
}
catch (Exception ex)
{
    logger.LogError(ex, "Agent run failed.");
    Environment.ExitCode = 1;
}
