# gRPC Demo Project

This project demonstrates various features and communication patterns of gRPC using .NET. It consists of a gRPC server (`GrpcServer`) and a gRPC client (`GrpcClient`) that interacts with the server.

## Features Demonstrated

The client and server showcase the following gRPC communication patterns and best practices:

1.  **Unary RPC**: The client sends a single request to the server and gets a single response back.
2.  **Server Streaming RPC**: The client sends a single request to the server and gets a stream of responses back.
3.  **Client Streaming RPC**: The client sends a stream of requests to the server and gets a single response back.
4.  **Bidirectional Streaming RPC**: The client sends a stream of requests to the server and gets a stream of responses back. The client and server can read and write messages independently.
5.  **Configurable Retries**: The client is configured with a global retry policy to automatically retry failed calls (e.g., on `Unavailable` status code) for all RPC methods.
6.  **TLS Secured Communication**: Server and client communication is secured using HTTPS (TLS).
7.  **Client-Side Deadlines**: All gRPC calls from the client are configured with a 10-second deadline.
8.  **Server-Side Logging Interceptor**: The server uses a custom interceptor to log details of incoming gRPC calls.
9.  **gRPC Health Check Service**: The server implements the standard gRPC Health Check service, and the client queries it before running demos.

## Prerequisites

Before running the application, ensure you have a trusted ASP.NET Core development certificate. If you haven't already, you can set one up by running the following command in your terminal:
dotnet dev-certs https --trust
This is necessary for the client to trust the server's TLS certificate.

## How to Run

To run this demo, you need to start the gRPC server first, and then run the gRPC client.

### 1. Run the gRPC Server

Open a terminal, navigate to the project's root directory, and run the following command:
dotnet run --project GrpcServer
The server will start and listen on `https://localhost:5001` using HTTPS.

### 2. Run the gRPC Client

Open another terminal, navigate to the project's root directory, and run the following command:
dotnet run --project GrpcClient
The client will first perform a health check and then make calls to the server demonstrating the different RPC patterns, using a default name ("World") for the requests.