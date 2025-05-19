using Grpc.Core;
using GrpcDemo;

namespace GrpcServer.Services;

public class GreeterService : GrpcDemo.Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    // Unary RPC
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Saying hello to {Name}", request.Name);
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }

    // Server Streaming RPC
    public override async Task LotsOfReplies(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Sending multiple replies to {Name}", request.Name);
        
        // Send 5 responses with a short delay between them
        for (int i = 1; i <= 5; i++)
        {
            // Check if client has cancelled the request
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await responseStream.WriteAsync(new HelloReply
            {
                Message = $"Hello {request.Name}, response #{i}"
            });

            // Simulate server processing time
            await Task.Delay(500, context.CancellationToken);
        }
    }

    // Client Streaming RPC
    public override async Task<HelloReply> LotsOfGreetings(IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
    {
        var names = new List<string>();
        
        // Read all incoming messages
        while (await requestStream.MoveNext())
        {
            var request = requestStream.Current;
            _logger.LogInformation("Received greeting from {Name}", request.Name);
            names.Add(request.Name);
        }

        // Create a combined response
        return new HelloReply
        {
            Message = $"Hello to {names.Count} people: {string.Join(", ", names)}"
        };
    }

    // Bidirectional Streaming RPC
    public override async Task BidiHello(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        // Process incoming messages as they arrive and respond immediately to each
        while (await requestStream.MoveNext())
        {
            var request = requestStream.Current;
            _logger.LogInformation("Bidirectional greeting for {Name}", request.Name);
            
            // Respond to each message
            await responseStream.WriteAsync(new HelloReply
            {
                Message = $"Hello {request.Name} (bidirectional at {DateTime.UtcNow:HH:mm:ss})"
            });
        }
    }
}
