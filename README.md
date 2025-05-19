# gRPC Demo Project

This project demonstrates various features and communication patterns of gRPC using .NET. It consists of a gRPC server (`GrpcServer`) and a gRPC client (`GrpcClient`) that interacts with the server.

## Features Demonstrated

The client and server showcase the following gRPC communication patterns:

1.  **Unary RPC**: The client sends a single request to the server and gets a single response back.
2.  **Server Streaming RPC**: The client sends a single request to the server and gets a stream of responses back.
3.  **Client Streaming RPC**: The client sends a stream of requests to the server and gets a single response back.
4.  **Bidirectional Streaming RPC**: The client sends a stream of requests to the server and gets a stream of responses back. The client and server can read and write messages independently.
5.  **Configurable Retries**: The client is configured with a global retry policy to automatically retry failed calls (e.g., on `Unavailable` status code) for all RPC methods.

## How to Run

To run this demo, you need to start the gRPC server first, and then run the gRPC client.

### 1. Run the gRPC Server

Open a terminal, navigate to the project's root directory, and run the following command:
dotnet run --project GrpcServer
The server will start and listen on `http://localhost:5000`.

### 2. Run the gRPC Client

Open another terminal, navigate to the project's root directory, and run the following command:
dotnet run --project GrpcClient
The client will then make calls to the server demonstrating the different RPC patterns.

You can also provide a name as a command-line argument to the client:
dotnet run --project GrpcClient -- "YourName"
If no name is provided, it will default to "World".