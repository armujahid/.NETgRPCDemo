using Grpc.Core;
using Grpc.Net.Client;
using GrpcDemo;
using Grpc.Net.Client.Configuration;

// See https://aka.ms/new-console-template for more information
// Process CLI arguments
if (args.Length > 0)
{
    // Use CLI argument as the name for the gRPC calls
    await RunGrpcDemosAsync(args[0]);
}
else
{
    Console.WriteLine("No name provided via command line. Using 'World' as default.");
    await RunGrpcDemosAsync("World");
}

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
    using var channel = GrpcChannel.ForAddress("http://localhost:5000", new GrpcChannelOptions
    {
        ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } }
    });
    var client = new Greeter.GreeterClient(channel);

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
    catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
    {
        Console.WriteLine($"Error occurred: {ex.Status.Detail} (Status: {ex.StatusCode})");
        Console.WriteLine("This likely means the server was unavailable and all retry attempts configured in the policy were exhausted.");
        Console.WriteLine("Make sure the gRPC server is running at http://localhost:5000");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        Console.WriteLine("Make sure the gRPC server is running at http://localhost:5000");
    }
}

// 1. Unary RPC: Client sends a single request and gets a single response
async Task DemoUnaryAsync(Greeter.GreeterClient client, string name)
{
    Console.WriteLine("1. Unary RPC Example:");
    Console.WriteLine("   Client sends one request, server sends one response\n");
    
    var reply = await client.SayHelloAsync(new HelloRequest { Name = name });
    Console.WriteLine($"   Response: {reply.Message}");
    Console.WriteLine();
}

// 2. Server Streaming RPC: Client sends a single request and gets a stream of responses
async Task DemoServerStreamingAsync(Greeter.GreeterClient client, string name)
{
    Console.WriteLine("2. Server Streaming RPC Example:");
    Console.WriteLine("   Client sends one request, server sends multiple responses\n");
    
    var call = client.LotsOfReplies(new HelloRequest { Name = name });
    
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
    
    using var call = client.LotsOfGreetings();
    
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
    
    using var call = client.BidiHello();
    
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
