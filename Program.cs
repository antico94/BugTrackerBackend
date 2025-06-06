// Program.cs (Add this configuration)

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Services;
using BugTracker.Services.Workflow;
using BugTracker.Models.Workflow;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure enum handling
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddLogging();

// Add Entity Framework
builder.Services.AddDbContext<BugTrackerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add existing services
builder.Services.AddScoped<TaskGenerationService>();
builder.Services.AddScoped<ExcelReportService>();

// Add new workflow services
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngineService>();
builder.Services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
builder.Services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();
builder.Services.AddScoped<IWorkflowRuleEngine, WorkflowRuleEngineService>();
builder.Services.AddScoped<WorkflowSeederService>();
builder.Services.AddScoped<WorkflowTaskGenerationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed workflow definitions on startup
using (var scope = app.Services.CreateScope())
{
    var workflowSeeder = scope.ServiceProvider.GetRequiredService<WorkflowSeederService>();
    try
    {
        await workflowSeeder.SeedWorkflowDefinitionsAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error seeding workflow definitions on startup");
    }
}

app.UseCors("AllowNextJS");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();