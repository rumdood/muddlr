using System.Reflection;
using System.Security.Claims;
using Serilog;
using System.Text.Json.Serialization;
using idunno.Authentication.Basic;
using Muddlr.Api;
using Muddlr.Api.Auth;
using Muddlr.Api.HealthStatus;
using Muddlr.WebFinger;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(builder.Configuration);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IWebFingerService, WebFingerService>();
builder.Services.AddTransient<WebFingerRequestHandler>();
builder.Services.AddLogging();
builder.Services.AddOutputCache(option =>
{
    option.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);
});
builder.Services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
    .AddBasic(options =>
    {
        options.Realm = "Basic Authentication";
        options.Events = new BasicAuthenticationEvents
        {
            OnValidateCredentials = context =>
            {
                var pwd = builder.Configuration.GetValue<string>(AuthConstants.Password);
                if (context.Username == builder.Configuration.GetValue<string>(AuthConstants.Username) &&
                    context.Password == builder.Configuration.GetValue<string>(AuthConstants.Password))
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String,
                            context.Options.ClaimsIssuer),
                        new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String,
                            context.Options.ClaimsIssuer)
                    };
                    context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                    context.Success();
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "Everybody",
        policy =>
        {
            policy.AllowAnyOrigin();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var useHttps = builder.Configuration.GetValue<bool>("muddlr:forceHttps");

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Error");

    if (useHttps)
    {
        app.UseHsts();
    }
}

app.UseCors();

if (useHttps)
{
    app.UseHttpsRedirection();
}

var apiAssembly = Assembly.GetExecutingAssembly().GetName();
var apiVersion = apiAssembly.Version is not null
    ? apiAssembly.Version.ToString()
    : "UNK";

var coreAssembly = typeof(WebFingerRecord).Assembly.GetName();
var coreVersion = coreAssembly.Version is not null
    ? coreAssembly.Version.ToString()
    : "UNK";

var muddlrStatus = new MuddlrStatus {ApiVersion = apiVersion, CoreVersion = coreVersion, Status = HealthStatus.Ok};

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/health"));
app.MapGet("/health", () => Results.Ok(muddlrStatus));
app.MapWebFingerApi();
app.MapWebFingerManagementApi();

app.Run();

public partial class Program { }