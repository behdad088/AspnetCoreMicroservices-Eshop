using Catalog.API.Entities;
using Catalog.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace Catalog.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CatalogController : ControllerBase
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(IProductRepository repository, ILogger<CatalogController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Product>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            _logger.LogInformation("Getting the list of all the products.");
            var products = await _repository.GetProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id:length(24)}", Name = "GetProduct")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(Product), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Product>> GetProductById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product Id cannot be null or empty.");

            _logger.LogInformation("Getting product with Id={ProductId}.", GetLogStringValue(id));

            var product = await _repository.GetProductAsync(id);
            if (product == null)
            {
                _logger.LogError("Product with id: {Id}, not found.", GetLogStringValue(id));
                return NotFound();
            }
            return Ok(product);
        }

        [Route("[action]/{category}", Name = "GetProductByCategory")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Product>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return BadRequest("Category cannot be null or empty.");

            _logger.LogInformation("Getting the list of all the products for Category={category}", GetLogStringValue(category));
            var products = await _repository.GetProductByCategoryAsync(category);
            return Ok(products);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Product), (int)HttpStatusCode.Created)]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
        {
            _logger.LogInformation($"Cearting product: {GetLogStringValue(JsonConvert.SerializeObject(product))}");
            await _repository.CreateProductAsync(product);
            return CreatedAtRoute("GetProduct", new { id = product.Id }, product);
        }

        [HttpPut]
        [ProducesResponseType(typeof(Product), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateProduct([FromBody] Product product)
        {
            _logger.LogInformation($"Updating product {GetLogStringValue(JsonConvert.SerializeObject(product))}");
            return Ok(await _repository.UpdateProductAsync(product));
        }

        [HttpDelete("{id:length(24)}", Name = "DeleteProduct")]
        [ProducesResponseType(typeof(Product), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteProductById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Product Id cannot be null or empty.");

            _logger.LogInformation($"Deleting product with Id {GetLogStringValue(id)}");
            return Ok(await _repository.DeleteProductAsync(id));
        }

        /// <summary>
        /// Prevents Log Injection attacks
        /// https://owasp.org/www-community/attacks/Log_Injection
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetLogStringValue(string value)
        {
            return value.Replace(Environment.NewLine, "");
        }
    }
}
