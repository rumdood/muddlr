using Serilog;
using System.Text.Json.Serialization;
using Muddlr.Api;
using Muddlr.Persons;
using Muddlr.WebFinger;

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

app.MapGet("/", () => Results.Ok("Muddlr 0.1 OK"));
app.MapPersonApi();
app.MapWebFingerApi();

app.Run();
