using Chatbot;
using Chatbot.Bot;
using Chatbot.Services;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.Agents.Builder;
using PromptEngineering.LLM.Configurations;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services
    .AddOptions<SpaceMissionsAgentOptions>()
    .Bind(builder.Configuration.GetSection(SpaceMissionsAgentOptions.SectionName))
    .PostConfigure(options => SpaceMissionsPathResolver.ApplyAbsolutePaths(options, builder.Environment.ContentRootPath));
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
    app.MapGet("/", () => "Space Missions Agent");
    app.UseDeveloperExceptionPage();
    app.MapControllers().AllowAnonymous();
}
else
{
    app.MapControllers();
}

app.Run();
