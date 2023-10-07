using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WebMVC.Extensions;
using WebMVC.Models;

namespace WebMVC.Services
{
    public class BasketService : IBasketService
    {
        private readonly HttpClient _client;
        private readonly ILogger<BasketService> _logger;

        public BasketService(HttpClient client, ILogger<BasketService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BasketModel> GetBasket(string username)
        {
            _logger.LogInformation("Getting basket by Username {Username}", username);
            var response = await _client.GetAsync($"/basket-api/api/v1/Basket/{username}");
            return await response.ReadContentAs<BasketModel>();
        }

        public async Task<BasketModel> UpdateBasket(BasketModel model)
        {
            _logger.LogInformation("Update basket for username {Username}", model?.Username);
            var response = await _client.PostAsJson($"/basket-api/api/v1/Basket", model);
            if (response.IsSuccessStatusCode)
                return await response.ReadContentAs<BasketModel>();
            else
            {
                _logger.LogWarning("Something went wrong when updating basket. Model {Model}", JsonConvert.SerializeObject(model));
                throw new Exception("Something went wrong when calling api.");
            }
        }

        public async Task CheckoutBasket(BasketCheckoutModel model)
        {
            _logger.LogInformation("Checkout basket for username {Username}", model?.Username);
            var response = await _client.PostAsJson($"/basket-api/api/v1/Basket/Checkout", model);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Something went wrong when checkOut basket. Model {Model}", JsonConvert.SerializeObject(model));
                throw new Exception("Something went wrong when calling api.");
            }
        }
    }
}
