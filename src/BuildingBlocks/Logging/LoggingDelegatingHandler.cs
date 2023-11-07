using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Eshop.BuildingBlocks.Logging
{
    /// <summary>
    /// Inspired by https://weblogs.asp.net/fredriknormen/log-message-request-and-response-in-asp-net-webapi
    /// </summary>
    public abstract class MessageHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger;

        public MessageHandler(ILogger<LoggingDelegatingHandler> logger)
        {
            _logger = logger ?? throw new Exception(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            try
            {
                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Environment.CurrentManagedThreadId);
                var requestInfo = string.Format("{0} {1}", request.Method, request.RequestUri);

                var requestMessage = request.Content != null ? await request.Content.ReadAsByteArrayAsync() : Array.Empty<byte>();

                await IncommingMessageAsync(corrId, requestInfo, requestMessage);

                var response = await base.SendAsync(request, cancellationToken);

                byte[] responseMessage;

                if (response.IsSuccessStatusCode)
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                else
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase ?? string.Empty);

                await OutgoingMessageAsync(corrId, requestInfo, responseMessage);

                return response;
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused)
            {
                string? hostWithPort;
                if (request.RequestUri != null)
                {
                    hostWithPort = request.RequestUri.IsDefaultPort
                   ? request.RequestUri.DnsSafeHost
                   : $"{request.RequestUri.DnsSafeHost}:{request.RequestUri.Port}";
                }
                else
                    hostWithPort = "NotAvailable";


                _logger.LogCritical(ex, "Connection to {Host} could not be established. Please verify the" +
                    " configuration to ensure that the service's URL has been correctly set.", hostWithPort);
            }

            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                RequestMessage = request
            };
        }

        protected abstract Task IncommingMessageAsync(string correlationId, string requestInfo, byte[] message);
        protected abstract Task OutgoingMessageAsync(string correlationId, string requestInfo, byte[] message);
    }

    public class LoggingDelegatingHandler : MessageHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger;

        public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger) : base(logger)
        {
            _logger = logger ?? throw new Exception(nameof(logger));
        }

        protected override async Task IncommingMessageAsync(string correlationId, string requestInfo, byte[] message)
        {
            await Task.Run(() =>
                _logger.LogInformation($"{correlationId} - Request: {requestInfo}\r\n{Encoding.UTF8.GetString(message)}"));
        }

        protected override async Task OutgoingMessageAsync(string correlationId, string requestInfo, byte[] message)
        {
            await Task.Run(() =>
                _logger.LogInformation($"{correlationId} - Response: {requestInfo}\r\n{Encoding.UTF8.GetString(message)}"));
        }
    }
}
