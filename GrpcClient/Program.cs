using Grpc.Core;
using Grpc.Net.Client;
using GrpcDemo;
using Grpc.Net.Client.Configuration;
using Grpc.Health.V1; // Added for Health Client

// See https://aka.ms/new-console-template for more information
// Hardcoded name for gRPC calls
const string defaultName = "World";
Console.WriteLine($"Using '{defaultName}' as the name for gRPC calls.");
await RunGrpcDemosAsync(defaultName);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

async Task RunGrpcDemosAsync(string name)
{
    // Configure retry policy
    var defaultMethodConfig = new MethodConfig
    {
        Names = { MethodName.Default },
        RetryPolicy = new RetryPolicy
        {
            MaxAttempts = 5,
            InitialBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(5),
            BackoffMultiplier = 1.5,
            RetryableStatusCodes = { StatusCode.Unavailable }
        }
    };

    // Create a channel to the gRPC server
    using var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
    {
        ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } }
    });
    var client = new Greeter.GreeterClient(channel);

    // Perform Health Check
    try
    {
        Console.WriteLine("\n=== Health Check ===");
        var healthClient = new Health.HealthClient(channel);
        var healthResponse = await healthClient.CheckAsync(new HealthCheckRequest { Service = "" }); // Empty service name checks overall server health
        Console.WriteLine($"Server health status: {healthResponse.Status}");
        // Optionally, check a specific service like "greet.Greeter"
        // var serviceHealthResponse = await healthClient.CheckAsync(new HealthCheckRequest { Service = "greet.Greeter" });
        // Console.WriteLine($"Greeter service health status: {serviceHealthResponse.Status}");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($"Health check failed: {ex.Status.Detail} (Status: {ex.StatusCode})");
    }
    Console.WriteLine(); // Add a newline for better separation

    Console.WriteLine("=== gRPC Demo Showcase ===");
    Console.WriteLine("Running all gRPC communication patterns...\n");
    Console.WriteLine("Note: All calls are configured with a retry policy for 'Unavailable' status codes.\n");
    
    try
    {
        // 1. Unary RPC
        await DemoUnaryAsync(client, name);

        // 2. Server Streaming RPC
        await DemoServerStreamingAsync(client, name);

        // 3. Client Streaming RPC
        await DemoClientStreamingAsync(client, name);

        // 4. Bidirectional Streaming RPC
        await DemoBidirectionalStreamingAsync(client, name);
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
    {
        Console.WriteLine($"A gRPC call exceeded its deadline: {ex.Status.Detail} (Status: {ex.StatusCode})");
        Console.WriteLine("This means the operation took longer than the 10-second limit.");
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
    {
        Console.WriteLine($"Error occurred: {ex.Status.Detail} (Status: {ex.StatusCode})");
        Console.WriteLine("This likely means the server was unavailable and all retry attempts configured in the policy were exhausted.");
        Console.WriteLine("Make sure the gRPC server is running at https://localhost:5001");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        Console.WriteLine("Make sure the gRPC server is running at https://localhost:5001");
    }
}

// 1. Unary RPC: Client sends a single request and gets a single response
async Task DemoUnaryAsync(Greeter.GreeterClient client, string name)
{
    Console.WriteLine("1. Unary RPC Example:");
    Console.WriteLine("   Client sends one request, server sends one response\n");
    
    var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10));
    var reply = await client.SayHelloAsync(new HelloRequest { Name = name }, callOptions);
    Console.WriteLine($"   Response: {reply.Message}");
    Console.WriteLine();
}

// 2. Server Streaming RPC: Client sends a single request and gets a stream of responses
async Task DemoServerStreamingAsync(Greeter.GreeterClient client, string name)
{
    Console.WriteLine("2. Server Streaming RPC Example:");
    Console.WriteLine("   Client sends one request, server sends multiple responses\n");
    
    var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10));
    var call = client.LotsOfReplies(new HelloRequest { Name = name }, callOptions);
    
    await foreach (var response in call.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"   Received: {response.Message}");
    }
    Console.WriteLine();
}

// 3. Client Streaming RPC: Client sends a stream of requests and gets a single response
async Task DemoClientStreamingAsync(Greeter.GreeterClient client, string name)
{
    Console.WriteLine("3. Client Streaming RPC Example:");
    Console.WriteLine("   Client sends multiple requests, server sends one response\n");
    
    var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10));
    using var call = client.LotsOfGreetings(callOptions);
    
    // Send multiple greetings
    var names = new List<string> { name, $"{name}'s friend", $"{name}'s family", $"{name}'s colleague", $"{name}'s neighbor" };
    
    foreach (var person in names)
    {
        Console.WriteLine($"   Sending greeting for: {person}");
        await call.RequestStream.WriteAsync(new HelloRequest { Name = person });
        await Task.Delay(200); // Small delay between messages
    }
    
    // Complete the call
    await call.RequestStream.CompleteAsync();
    var response = await call.ResponseAsync;
    
    Console.WriteLine($"   Final response: {response.Message}");
    Console.WriteLine();
}

// 4. Bidirectional Streaming RPC: Both client and server send a stream of messages
async Task DemoBidirectionalStreamingAsync(Greeter.GreeterClient client, string name)
{
    Console.WriteLine("4. Bidirectional Streaming RPC Example:");
    Console.WriteLine("   Client and server both send multiple messages\n");
    
    var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10));
    using var call = client.BidiHello(callOptions);
    
    // Start a task to read responses
    var responseTask = Task.Run(async () =>
    {
        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine($"   Received: {response.Message}");
        }
    });
    
    // Send requests
    var messages = new[] { 
        $"{name} says hi!", 
        $"{name} is testing bidirectional streaming", 
        $"{name} appreciates the demo",
        $"{name} says goodbye!"
    };
    
    foreach (var message in messages)
    {
        Console.WriteLine($"   Sending: {message}");
        await call.RequestStream.WriteAsync(new HelloRequest { Name = message });
        await Task.Delay(500);
    }
    
    // Complete the call from client side
    await call.RequestStream.CompleteAsync();
    
    // Wait for the server to complete sending responses
    await responseTask;
    Console.WriteLine();
}
