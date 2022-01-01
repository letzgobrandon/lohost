using lohost.API.Controllers;
using lohost.API.Hubs;
using lohost.API.Models;
using lohost.Logging;

var builder = WebApplication.CreateBuilder(args);

var hostingLocation = builder.Configuration["Hosting:Location"];

var systemLogging = new SystemLogging();
var localIntegrationHub = new LocalApplicationHub(systemLogging);

// Might as well open it up
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Add SignalR
builder.Services.AddSignalR(configure => { configure.MaximumReceiveMessageSize = null; });
builder.Services.AddSingleton<LocalApplicationHub>(t => localIntegrationHub);

// Build the app and configure the required services
var app = builder.Build();

app.UseHttpsRedirection();

app.MapHub<LocalApplicationHub>("/ApplicationHub");

app.MapGet("{*.}", async (HttpContext httpContext) =>
{
    var urlHost = httpContext.Request.Host.ToString();
    var queryPath = httpContext.Request.Path.ToString();

    var applicationId = urlHost.Replace(hostingLocation, string.Empty, StringComparison.OrdinalIgnoreCase).Trim('.');

    // This is a request for a remote application.
    if (!string.IsNullOrEmpty(applicationId))
    {
        LocalApplication localApplication = new LocalApplication(systemLogging, localIntegrationHub);

        DocumentResponse documentResponse = await localApplication.GetDocument(applicationId, queryPath);

        httpContext.Response.ContentType = "text/html";
        
        return System.Text.Encoding.Default.GetString(documentResponse.DocumentData);
    }
    else
    {
        Console.WriteLine("local");

        return null;
    }
});

app.Run();