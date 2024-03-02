using AutoMapper;
using Basket.API.Entities;
using Basket.API.Repositories;
using Basket.Grpc.Protos;
using Discount.Grpc.Protos;
using Eshop.BuildingBlocks.EventBus.RabbitMQ.Abstractions;
using Grpc.Core;

namespace Basket.API.Grpc
{
    public class BasketService : BasketProtoService.BasketProtoServiceBase
    {
        private readonly IRabbitMQProducer _rabbitMQProducer;
        private readonly IBasketRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<BasketService> _logger;
        private readonly DiscountProtoService.DiscountProtoServiceClient _discountProtoServiceClient;

        public BasketService(
            IRabbitMQProducer rabbitMQProducer,
            IBasketRepository repository,
            IMapper mapper,
            ILogger<BasketService> logger,
            DiscountProtoService.DiscountProtoServiceClient discountProtoServiceClient)
        {
            _rabbitMQProducer = rabbitMQProducer ?? throw new ArgumentNullException(nameof(rabbitMQProducer));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _discountProtoServiceClient = discountProtoServiceClient ?? throw new ArgumentNullException(nameof(discountProtoServiceClient));
        }

        public override async Task<ShoppingCartModel> GetBasket(GetBasketRequest request, ServerCallContext context)
        {
            if (string.IsNullOrEmpty(request.Username))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Username cannot be null or empty."));

            _logger.LogInformation($"Getting basket for username {request.Username}");
            var basket = await _repository.GetBasketAsync(request.Username);
            basket ??= new ShoppingCart(request.Username);
            _logger.LogInformation("Basket is retrieved for Username : {Username}", request.Username);

            var shoppingCartModel = _mapper.Map<ShoppingCartModel>(basket);
            return shoppingCartModel;
        }

        public override async Task<ShoppingCartModel> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Updating basket for username {request.ShoppingCart.Username}");
            var ShoppingCartItems = request.ShoppingCart.Items.ToList();

            foreach (var item in ShoppingCartItems)
            {
                var discountResquest = new GetDiscountRequest() { ProductName = item.ProductName };
                var discount = await _discountProtoServiceClient.GetDiscountAsync(discountResquest);
                item.Price -= discount.Amount;
            }

            var shoppingCart = _mapper.Map<Entities.ShoppingCart>(request.ShoppingCart);
            shoppingCart = await _repository.UpdateBasketAsync(shoppingCart);
            var shoppingCartModel = _mapper.Map<ShoppingCartModel>(shoppingCart);
            return shoppingCartModel;
        }

        public override async Task<NoResponse> DeletetBasket(DeletetBasketRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Delete basket for username {request.Username}");
            await _repository.DeleteBasketAsync(request.Username);
            return new NoResponse();
        }

        public override async Task<NoResponse> Checkout(CheckoutViewModel request, ServerCallContext context)
        {
            var basket = await _repository.GetBasketAsync(request.Username);
            if (basket == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Basket not found."));

            _logger.LogInformation($"checking out basket for username {request.Username}");
            request.TotalPrice = (double)basket.TotalPrice;
            var basketCheckout = _mapper.Map<BasketCheckout>(request);

            await _rabbitMQProducer.PublishAsJsonAsync("order.checkout", basketCheckout);
            await _repository.DeleteBasketAsync(basket.Username);

            return new NoResponse();
        }
    }
}
