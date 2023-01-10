using System.Reflection;
using Serilog;
using System.Text.Json.Serialization;
using Muddlr.Api;
using Muddlr.Api.HealthStatus;
using Muddlr.Users;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<WebFingerRequestHandler>();
builder.Services.AddLogging();

var connString = builder.Configuration.GetConnectionString("UserDb");
if (string.IsNullOrEmpty(connString))
{
    builder.Services.AddSingleton<IUserRepository, FileSystemDataSource>();
}
else
{
    builder.Services.AddSingleton<IUserRepository, LiteDbDataSource>(_ => new LiteDbDataSource(connString));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var apiAssembly = Assembly.GetExecutingAssembly().GetName();
var apiVersion = apiAssembly.Version is not null
    ? apiAssembly.Version.ToString()
    : "UNK";

var coreAssembly = typeof(User).Assembly.GetName();
var coreVersion = coreAssembly.Version is not null
    ? coreAssembly.Version.ToString()
    : "UNK";

var muddlrStatus = new MuddlrStatus {ApiVersion = apiVersion, CoreVersion = coreVersion, Status = HealthStatus.Ok};

app.MapGet("/", () => Results.Redirect("/health"));
app.MapGet("/health", () => Results.Ok(muddlrStatus));
app.MapUserApi();
app.MapWebFingerApi();

app.Run();

public partial class Program { }