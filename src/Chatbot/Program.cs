using System.Reflection;
using Chatbot;
using Chatbot.Bot;
using Chatbot.Services;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.Agents.Builder;
using PromptEngineering.LLM.Configurations;
using Rag;

var builder = WebApplication.CreateBuilder(args);

// Overrides appsettings.json; uses UserSecretsId from Chatbot.csproj (dotnet user-secrets set).
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

builder.Services.AddControllers();
builder.Services.AddHttpClient("WebClient", client => client.Timeout = TimeSpan.FromSeconds(600));
builder.Services.AddHttpContextAccessor();
builder.Services.AddCloudAdapter();
builder.Logging.AddConsole();

builder.Services.AddBotAspNetAuthentication(builder.Configuration);
builder.Services.AddSingleton<IStorage, MemoryStorage>();

builder.AddAgentApplicationOptions();
builder.AddAgent<EchoBot>();

builder.Services.AddGenAi(builder.Configuration);
builder.Services.AddRag(builder.Configuration);
builder.Services.PostConfigure<RagSettings>(options =>
    RagPathResolver.ApplyAbsolutePaths(options, builder.Environment.ContentRootPath));
builder.Services.AddSingleton<RagIndexStore>();
builder.Services.AddHostedService<RagIndexBackgroundService>();
builder.Services
    .AddOptions<SpaceMissionsAgentOptions>()
    .Bind(builder.Configuration.GetSection(SpaceMissionsAgentOptions.SectionName))
    .PostConfigure(options => SpaceMissionsPathResolver.ApplyAbsolutePaths(
        options,
        builder.Environment.ContentRootPath,
        AppContext.BaseDirectory));
builder.Services.AddSingleton<ISpaceMissionsMcpAgentService, SpaceMissionsMcpAgentService>();
builder.Services.AddScoped<SpaceMissionsAgentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
});

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Playground")
{
    app.MapGet("/", () => "Space Missions Agent (RAG + MCP)");
    app.MapGet("/ready", (RagIndexStore store) =>
    {
        if (store.IsReady)
            return Results.Ok(new { status = "ready", chunks = store.Index!.Count });

        if (store.IsBuilding)
            return Results.Json(new { status = "building" }, statusCode: StatusCodes.Status503ServiceUnavailable);

        return Results.Json(
            new { status = "failed", error = store.BuildError?.Message ?? "Index unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    });
    app.UseDeveloperExceptionPage();
    app.MapControllers().AllowAnonymous();
}
else
{
    app.MapControllers();
}

app.Run();
