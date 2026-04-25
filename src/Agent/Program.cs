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
builder.Services.AddScoped<WeatherNewsAgentService>();

var app = builder.Build();

if (args.Length >= 1 && args[0].Equals("--eval", StringComparison.OrdinalIgnoreCase))
{
    await using var evalScope = app.Services.CreateAsyncScope();
    await RunEvalAsync(evalScope.ServiceProvider).ConfigureAwait(false);
    return;
}

var question = args.Length > 0 ? string.Join(" ", args) : null;
if (string.IsNullOrWhiteSpace(question))
{
    Console.WriteLine("Usage: Agent [--eval] [question...]");
    Console.WriteLine("Example: Agent \"What is the weather and the latest news in Paris?\"");
    Console.WriteLine("Or run with --eval to score tool routing on the benchmark JSON.");
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

static async Task RunEvalAsync(IServiceProvider services)
{
    var agent = services.GetRequiredService<WeatherNewsAgentService>();
    var mapper = services.GetRequiredService<ToolDomainMapper>();
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Eval");

    var path = Path.Combine(AppContext.BaseDirectory, "data", "eval_items.json");
    if (!File.Exists(path))
    {
        logger.LogError("Eval dataset not found at {Path}", path);
        Environment.ExitCode = 1;
        return;
    }

    var dataset = await EvalDatasetLoader.LoadAsync(path).ConfigureAwait(false);
    Console.WriteLine("Tool Routing Accuracy (TRA) benchmark");
    Console.WriteLine("Dataset: {0}", path);
    Console.WriteLine();

    var scores = new List<int>();
    foreach (var item in dataset.Items)
    {
        try
        {
            var result = await agent.RunAsync(item.Question).ConfigureAwait(false);
            var pass = ToolRoutingEvaluator.Passes(item.ExpectedDomains, result.ToolNamesInvoked, mapper);
            var s = pass ? 1 : 0;
            scores.Add(s);
            var tools = result.ToolNamesInvoked.Count > 0 ? string.Join(", ", result.ToolNamesInvoked) : "(none)";
            Console.WriteLine("{0}  score={1}  tools=[{2}]", item.Id, s, tools);
        }
        catch (Exception ex)
        {
            scores.Add(0);
            Console.WriteLine("{0}  score=0  ERROR: {1}", item.Id, ex.Message);
        }
    }

    var mean = scores.Count > 0 ? scores.Average() : 0d;
    Console.WriteLine();
    Console.WriteLine("Overall TRA (mean): {0:F3}", mean);
}
