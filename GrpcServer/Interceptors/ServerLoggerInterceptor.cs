using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GrpcServer.Interceptors
{
    public class ServerLoggerInterceptor : Interceptor
    {
        private readonly ILogger<ServerLoggerInterceptor> _logger;

        public ServerLoggerInterceptor(ILogger<ServerLoggerInterceptor> logger)
        {
            _logger = logger;
        }

        private void LogCallDetails<TRequest, TResponse>(MethodType methodType, ServerCallContext context, TRequest request)
            where TRequest : class
            where TResponse : class
        {
            _logger.LogInformation(
                "Starting call. Type: {MethodType}, Method: {MethodName}, Peer: {Peer}, Request Type: {RequestType}",
                methodType,
                context.Method,
                context.Peer,
                typeof(TRequest).Name);
        }

        private void LogCallCompletion(ServerCallContext context, Status status)
        {
            _logger.LogInformation(
                "Completed call. Method: {MethodName}, Status: {StatusCode}, Detail: {StatusDetail}",
                context.Method,
                status.StatusCode,
                status.Detail);
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            LogCallDetails<TRequest, TResponse>(MethodType.Unary, context, request);
            try
            {
                var response = await continuation(request, context);
                LogCallCompletion(context, context.Status);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Unary call: {MethodName}", context.Method);
                LogCallCompletion(context, new Status(StatusCode.Internal, ex.Message));
                throw;
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            _logger.LogInformation("Starting Client Streaming call. Method: {MethodName}, Peer: {Peer}", context.Method, context.Peer);
            // Cannot log request details here as it's a stream. Logging request type in continuation.
            try
            {
                var response = await continuation(new LoggingAsyncStreamReader<TRequest>(requestStream, _logger, context.Method, true), context);
                LogCallCompletion(context, context.Status);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Client Streaming call: {MethodName}", context.Method);
                LogCallCompletion(context, new Status(StatusCode.Internal, ex.Message));
                throw;
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogCallDetails<TRequest, TResponse>(MethodType.ServerStreaming, context, request);
            try
            {
                await continuation(request, new LoggingServerStreamWriter<TResponse>(responseStream, _logger, context.Method, false), context);
                LogCallCompletion(context, context.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Server Streaming call: {MethodName}", context.Method);
                LogCallCompletion(context, new Status(StatusCode.Internal, ex.Message));
                throw;
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            _logger.LogInformation("Starting Bidirectional Streaming call. Method: {MethodName}, Peer: {Peer}", context.Method, context.Peer);
            // Cannot log request details here as it's a stream. Logging request type in continuation.
            try
            {
                await continuation(new LoggingAsyncStreamReader<TRequest>(requestStream, _logger, context.Method, true), 
                                   new LoggingServerStreamWriter<TResponse>(responseStream, _logger, context.Method, false), 
                                   context);
                LogCallCompletion(context, context.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Bidirectional Streaming call: {MethodName}", context.Method);
                LogCallCompletion(context, new Status(StatusCode.Internal, ex.Message));
                throw;
            }
        }
    }

    // Helper class to log messages as they are read from the client stream
    internal class LoggingAsyncStreamReader<T> : IAsyncStreamReader<T> where T : class
    {
        private readonly IAsyncStreamReader<T> _inner;
        private readonly ILogger _logger;
        private readonly string _methodName;
        private readonly bool _isRequest; // True if logging requests, false for responses

        public LoggingAsyncStreamReader(IAsyncStreamReader<T> inner, ILogger logger, string methodName, bool isRequest)
        {
            _inner = inner;
            _logger = logger;
            _methodName = methodName;
            _isRequest = isRequest;
        }

        public T Current => _inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken = default)
        {
            var hasNext = await _inner.MoveNext(cancellationToken);
            if (hasNext)
            {
                if (_isRequest)
                {
                     _logger.LogInformation("Method: {MethodName}, Client Stream Received: {RequestType}", _methodName, typeof(T).Name);
                }
                // else: server is sending, not receiving. Logging for responses is handled by LoggingServerStreamWriter
            }
            return hasNext;
        }
    }

    // Helper class to log messages as they are written to the client
    internal class LoggingServerStreamWriter<T> : IServerStreamWriter<T> where T : class
    {
        private readonly IServerStreamWriter<T> _inner;
        private readonly ILogger _logger;
        private readonly string _methodName;
        private readonly bool _isRequest; // Should be false for server stream writer

        public LoggingServerStreamWriter(IServerStreamWriter<T> inner, ILogger logger, string methodName, bool isRequest)
        {
            _inner = inner;
            _logger = logger;
            _methodName = methodName;
            _isRequest = isRequest; // Typically false for server writing responses
        }

        public WriteOptions WriteOptions
        {
            get => _inner.WriteOptions;
            set => _inner.WriteOptions = value;
        }

        public async Task WriteAsync(T message)
        {
            if (!_isRequest) // Logging responses from server
            {
                _logger.LogInformation("Method: {MethodName}, Server Stream Sending: {ResponseType}", _methodName, typeof(T).Name);
            }
            await _inner.WriteAsync(message);
        }
    }
}
