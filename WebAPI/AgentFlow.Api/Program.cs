using AgentFlow.Api.Agents;
using AgentFlow.Api.Data;
using AgentFlow.Api.Engine;
using AgentFlow.Api.Seeding;
using AgentFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<WorkflowRepository>();
builder.Services.AddHttpClient<GroqService>();
builder.Services.AddHttpClient("agents", c => c.Timeout = TimeSpan.FromSeconds(60));

builder.Services.AddSingleton<IAgent, LlmAgent>();
builder.Services.AddSingleton<IAgent, DataExtractorAgent>();
builder.Services.AddSingleton<IAgent, AnalyzerAgent>();
builder.Services.AddSingleton<IAgent, ReportWriterAgent>();
builder.Services.AddSingleton<IAgent, EmailSenderAgent>();
builder.Services.AddSingleton<IAgent, HttpAgent>();
builder.Services.AddSingleton<AgentRegistry>();

builder.Services.AddSingleton<WorkflowEngine>();
builder.Services.AddSingleton<RunQueue>();
builder.Services.AddHostedService<RunWorker>();

// ---------- Controllers ----------
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Match the UI: camelCase JSON, but tolerate PascalCase inbound.
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
              ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<WorkflowRepository>();
    var reg  = scope.ServiceProvider.GetRequiredService<AgentRegistry>();
    try { await SampleWorkflows.SeedAsync(repo, reg); }
    catch (Exception ex) { app.Logger.LogWarning(ex, "Seeding skipped (DB not ready?)."); }
}

app.Run();