using Basket.API.Entities;
using Basket.API.Repositories;
using Discount.Grpc.Protos;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;
        private readonly DiscountProtoService.DiscountProtoServiceClient _discountProtoServiceClient;

        public BasketController(IBasketRepository repository, DiscountProtoService.DiscountProtoServiceClient discountProtoServiceClient)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _discountProtoServiceClient = discountProtoServiceClient ?? throw new ArgumentNullException(nameof(discountProtoServiceClient));
        }

        [HttpGet("{username}", Name = "GetBasket")]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> GetBasket(string username)
        {
            var basket = await _repository.GetBasketAsync(username);
            return Ok(basket ?? new ShoppingCart(username));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> UpdateBasket([FromBody] ShoppingCart basket)
        {
            // TODO : Communicate with Discount.Grpc
            // and Calculate latest prices of product into shopping cart
            // consume Discount Grpc
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
    }
}
