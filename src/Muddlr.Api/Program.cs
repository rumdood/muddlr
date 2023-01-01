using System.Reflection;
using Serilog;
using System.Text.Json.Serialization;
using Muddlr.Api;
using Muddlr.Persons;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Host.UseSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IPersonRepository, LiteDbDataSource>();
builder.Services.AddTransient<WebFingerRequestHandler>();

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

var coreAssembly = typeof(Person).Assembly.GetName();
var coreVersion = coreAssembly.Version is not null
    ? coreAssembly.Version.ToString()
    : "UNK";

app.MapGet("/", () => Results.Redirect("/health"));
app.MapGet("/health", () => Results.Ok($"Muddlr API Version: {apiVersion}, Core Version: {coreVersion} OK"));
app.MapPersonApi();
app.MapWebFingerApi();

app.Run();


public partial class Program { }