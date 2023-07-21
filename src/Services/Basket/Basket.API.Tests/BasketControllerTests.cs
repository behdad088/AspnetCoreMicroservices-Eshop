using Basket.API.Controllers;
using Basket.API.Entities;
using Basket.API.Repositories;
using Basket.API.Tests.Helpers;
using Discount.Grpc.Protos;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Basket.API.Tests
{
    public class BasketControllerTests
    {
        private readonly Mock<IBasketRepository> _basketRepositoryMock;
        private readonly Mock<IRabbitMQProducer> _rabbitMQProducerMock;
        private readonly Mock<DiscountProtoService.DiscountProtoServiceClient> _discountProtoServiceClientMock;

        public BasketControllerTests()
        {
            _basketRepositoryMock = new Mock<IBasketRepository>();
            _rabbitMQProducerMock = new Mock<IRabbitMQProducer>();
            _discountProtoServiceClientMock = new Mock<DiscountProtoService.DiscountProtoServiceClient>();
        }

        [Fact]
        public async Task GetBasket_Username_Null_Or_Empty_Return_BadRequest()
        {
            // Arrange
            var username = string.Empty;
            var controller = new BasketController(_basketRepositoryMock.Object, _discountProtoServiceClientMock.Object, _rabbitMQProducerMock.Object);

            // Act
            var actual = await controller.GetBasket(username);

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual.Result);
        }

        [Fact]
        public async Task GetBasket_NoBasket_Return_EmptyBasket()
        {
            // Arrange
            var username = "test";
            _basketRepositoryMock.Setup(x => x.GetBasketAsync(It.IsAny<string>())).ReturnsAsync(() => null);
            var controller = new BasketController(_basketRepositoryMock.Object, _discountProtoServiceClientMock.Object, _rabbitMQProducerMock.Object);

            // Act
            var actual = await controller.GetBasket(username);

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ActionResult<ShoppingCart>>(actual);
            Assert.Equal((actual.Result as OkObjectResult).StatusCode, (int)System.Net.HttpStatusCode.OK);
            Assert.Equal(username, (((OkObjectResult)actual.Result).Value as ShoppingCart).Username);
        }

        [Fact]
        public async Task UpdateBasket_Returns_Updated_Basket()
        {
            // Arrange
            var basket = new ShoppingCart()
            {
                Username = "test",
                Items = new List<ShoppingCartItem> { new ShoppingCartItem()
                {
                    Color = "test",
                    Price = 1000,
                    ProductName = "test",
                    Quantity = 1
                } }
            };

            var discountMockCall = CallHelpers.CreateAsyncUnaryCall(new CouponModel() { Id = 1, Amount = 100, ProductName = "test" });
            _discountProtoServiceClientMock.Setup(x => x.GetDiscountAsync(It.IsAny<GetDiscountRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Returns(discountMockCall);

            _basketRepositoryMock.Setup(x => x.UpdateBasketAsync(It.IsAny<ShoppingCart>())).ReturnsAsync(basket);
            var controller = new BasketController(_basketRepositoryMock.Object, _discountProtoServiceClientMock.Object, _rabbitMQProducerMock.Object);

            // Act
            var actual = await controller.UpdateBasket(basket);

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ActionResult<ShoppingCart>>(actual);
            Assert.Equal((actual.Result as OkObjectResult).StatusCode, (int)System.Net.HttpStatusCode.OK);
            Assert.Equal(900, (((OkObjectResult)actual.Result).Value as ShoppingCart).TotalPrice);
            _discountProtoServiceClientMock.Verify(x => x.GetDiscountAsync(It.IsAny<GetDiscountRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Checkout_Return_BadRequest_When_Basket_Is_Null()
        {
            // Arrange
            var basket = new BasketCheckout()
            {
                Username = "test",
            };
            _basketRepositoryMock.Setup(x => x.GetBasketAsync(It.IsAny<string>())).ReturnsAsync(() => null);
            var controller = new BasketController(_basketRepositoryMock.Object, _discountProtoServiceClientMock.Object, _rabbitMQProducerMock.Object);

            // Act 
            var actual = await controller.Checkout(basket);

            // Assert 
            Assert.IsType<BadRequestResult>(actual);
        }

        [Fact]
        public async Task Checkout_Sends_Out_CheckOut_Event_And_Deletes_Basket()
        {
            // Arrange
            var basket = new BasketCheckout()
            {
                Username = "test",
            };
            _basketRepositoryMock.Setup(x => x.GetBasketAsync(It.IsAny<string>())).ReturnsAsync(() => new ShoppingCart()
            {
                Username = "test",
                Items = new List<ShoppingCartItem>
                {
                    new ShoppingCartItem()
                    {
                        Color = "test",
                        Price = 1000,
                        ProductName = "test",
                        Quantity = 1
                    }
                }
            });

            var controller = new BasketController(_basketRepositoryMock.Object, _discountProtoServiceClientMock.Object, _rabbitMQProducerMock.Object);
            _rabbitMQProducerMock.Setup(x => x.PublishAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()));
            _basketRepositoryMock.Setup(x => x.DeleteBasketAsync(It.IsAny<string>()));

            // Act 
            var actual = await controller.Checkout(basket);

            // Assert 
            Assert.IsType<AcceptedResult>(actual);
            _rabbitMQProducerMock.Verify(x => x.PublishAsJsonAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
            _basketRepositoryMock.Verify(x => x.DeleteBasketAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
