using GrpcServer.Interceptors; // Added for interceptor
using GrpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ServerLoggerInterceptor>(); // Register interceptor
});
builder.Services.AddSingleton<ServerLoggerInterceptor>(); // Add interceptor to DI
builder.Services.AddGrpcHealthChecks(); // Add Health Checks services

// Configure Kestrel to listen on port 5001 for HTTP/2 with TLS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
        listenOptions.UseHttps(); // Enable HTTPS
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcHealthChecksService(); // Map Health Checks service endpoint
app.MapGet("/", () => "gRPC Demo Server - Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
