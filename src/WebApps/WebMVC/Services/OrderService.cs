using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WebMVC.Extensions;
using WebMVC.Models;

namespace WebMVC.Services
{
    public class OrderService : IOrderService
    {
        private readonly HttpClient _client;
        private readonly ILogger<OrderService> _logger;

        public OrderService(HttpClient client, ILogger<OrderService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<OrderResponseModel>> GetOrdersByUserName(string username)
        {
            _logger.LogInformation("Getting order by username {Username}", username);
            var response = await _client.GetAsync($"/ordering-api/api/v1/Order/{username}");
            return await response.ReadContentAs<List<OrderResponseModel>>();
        }
    }
}
