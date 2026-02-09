using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Automation.Domain;
using ErpSystem.Automation.Infrastructure;
using ErpSystem.Automation.Application;
using MediatR;
using Dapr.Client;
using ErpSystem.BuildingBlocks;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Dapr Client
var daprClient = new DaprClientBuilder().Build();

// Fetch connection string from Dapr Secrets with retry
string? connectionString = null;
for (int i = 0; i < 5; i++)
{
    try
    {
        var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:automationdb");
        connectionString = secrets.Values.FirstOrDefault();
        if (!string.IsNullOrEmpty(connectionString)) break;
    }
    catch { await Task.Delay(1000); }
}

if (string.IsNullOrEmpty(connectionString))
    connectionString = builder.Configuration.GetConnectionString("automationdb");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<AutomationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Dapr
builder.Services.AddDaprClient();

// BuildingBlocks
builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
builder.Services.AddDaprEventBus();

// MediatR - MUST be before IPublisher!
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AutomationEngine).Assembly));

// IPublisher (depends on MediatR)
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp =>
    new EventStore(
        sp.GetRequiredService<AutomationDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Automation.Domain.{name}, ErpSystem.Automation")!));

// Register repositories and engine
builder.Services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
builder.Services.AddScoped<IActionExecutor, ActionExecutor>();
builder.Services.AddScoped<AutomationEngine>();

builder.Services.AddHttpClient();

// Placeholder services for ActionExecutor
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Ensure databases created
using (IServiceScope scope = app.Services.CreateScope())
{
    AutomationDbContext db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace ErpSystem.Automation.Infrastructure
{
    public class AutomationDbContext(DbContextOptions<AutomationDbContext> options) : DbContext(options)
    {
        public DbSet<EventStream> Events { get; set; } = null!;
        public DbSet<AutomationRuleReadModel> Rules { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventStream>(b =>
            {
                b.HasKey(e => new { e.AggregateId, e.Version });
                b.Property(e => e.Payload).HasColumnType("jsonb");
            });

            modelBuilder.Entity<AutomationRuleReadModel>().HasKey(x => x.Id);
        }
    }

    public class AutomationRuleReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TriggerEventType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AutomationRuleRepository(AutomationDbContext context, IEventStore eventStore) : IAutomationRuleRepository
    {
        public async Task<List<AutomationRule>> GetActiveRulesByEventType(string eventType)
        {
            List<Guid> ruleIds = await context.Rules
                .Where(r => r.TriggerEventType == eventType && r.IsActive)
                .Select(r => r.Id)
                .ToListAsync();

            List<AutomationRule> rules = [];
            foreach (Guid id in ruleIds)
            {
                AutomationRule? rule = await eventStore.LoadAggregateAsync<AutomationRule>(id);
                if (rule != null)
                {
                    rules.Add(rule);
                }
            }

            return rules;
        }
    }

    public class EmailService(ILogger<EmailService> logger) : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            logger.LogInformation("Sending Email to {To}: {Subject}\n{Body}", to, subject, body);
            return Task.CompletedTask;
        }
    }

    public class NotificationService(ILogger<NotificationService> logger) : INotificationService
    {
        public Task SendNotificationAsync(string channel, string message)
        {
            logger.LogInformation("Sending {Channel} Notification: {Message}", channel, message);
            return Task.CompletedTask;
        }
    }
}
