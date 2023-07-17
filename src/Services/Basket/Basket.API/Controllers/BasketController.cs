using Basket.API.Entities;
using Basket.API.Repositories;
using Discount.Grpc.Protos;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;
        private readonly IRabbitMQProducer _rabbitMQProducer;
        private readonly DiscountProtoService.DiscountProtoServiceClient _discountProtoServiceClient;

        public BasketController(
            IBasketRepository repository,
            DiscountProtoService.DiscountProtoServiceClient discountProtoServiceClient,
            IRabbitMQProducer rabbitMQProducer)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _discountProtoServiceClient = discountProtoServiceClient ?? throw new ArgumentNullException(nameof(discountProtoServiceClient));
            _rabbitMQProducer = rabbitMQProducer ?? throw new ArgumentNullException(nameof(rabbitMQProducer));
        }

        [HttpGet("{username}", Name = "GetBasket")]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> GetBasket(string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest("Username cannot be null or empty.");

            var basket = await _repository.GetBasketAsync(username);
            return Ok(basket ?? new ShoppingCart(username));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> UpdateBasket([FromBody] ShoppingCart basket)
        {
            foreach (var item in basket.Items)
            {
                var discountResquest = new GetDiscountRequest() { ProductName = item.ProductName };
                var discount = await _discountProtoServiceClient.GetDiscountAsync(discountResquest);
                item.Price -= discount.Amount;
            }

            return Ok(await _repository.UpdateBasketAsync(basket));
        }

        [HttpDelete("{username}", Name = "DeleteBasket")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteBasket(string username)
        {
            await _repository.DeleteBasketAsync(username);
            return Ok();
        }

        [Route("[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Checkout([FromBody] BasketCheckout basketCheckout)
        {
            var basket = await _repository.GetBasketAsync(basketCheckout.Username);
            if (basket == null)
                return BadRequest();

            basketCheckout.TotalPrice = basket.TotalPrice;
            await _rabbitMQProducer.PublishAsJsonAsync("order.checkout", basketCheckout);

            await _repository.DeleteBasketAsync(basket.Username);

            return Accepted();
        }
    }
}
