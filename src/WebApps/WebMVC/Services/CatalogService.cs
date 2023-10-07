using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WebMVC.Extensions;
using WebMVC.Models;

namespace WebMVC.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly HttpClient _client;
        private readonly ILogger<CatalogService> _logger;

        public CatalogService(HttpClient client, ILogger<CatalogService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<CatalogModel>> GetCatalog()
        {
            _logger.LogInformation("Getting the list of catalog.");
            var response = await _client.GetAsync("/catalog-api/api/v1/Catalog");
            return await response.ReadContentAs<List<CatalogModel>>();
        }

        public async Task<CatalogModel> GetCatalog(string id)
        {
            _logger.LogInformation("Getting catalog with Id {Id}", id);
            var response = await _client.GetAsync($"/catalog-api/api/v1/Catalog/{id}");
            return await response.ReadContentAs<CatalogModel>();
        }

        public async Task<IEnumerable<CatalogModel>> GetCatalogByCategory(string category)
        {
            _logger.LogInformation("Getting catalog by category {category}", category);
            var response = await _client.GetAsync($"/catalog-api/api/v1/Catalog/GetProductByCategory/{category}");
            return await response.ReadContentAs<List<CatalogModel>>();
        }

        public async Task<CatalogModel> CreateCatalog(CatalogModel model)
        {
            _logger.LogInformation("Creating catalog with Model {Model}", JsonConvert.SerializeObject(model));
            var response = await _client.PostAsJson($"/catalog-api/api/v1/Catalog", model);
            if (response.IsSuccessStatusCode)
                return await response.ReadContentAs<CatalogModel>();
            else
            {
                _logger.LogWarning("Something went wrong when creating catalog. Model {Model}", JsonConvert.SerializeObject(model));
                throw new Exception("Something went wrong when calling api.");
            }
        }
    }
}
