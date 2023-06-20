using AutoMapper;
using Basket.API.Repositories;
using Basket.Grpc.Protos;
using Discount.Grpc.Protos;
using Grpc.Core;

namespace Basket.API.Grpc
{
    public class BasketService : BasketProtoService.BasketProtoServiceBase
    {
        private readonly IBasketRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<BasketService> _logger;
        private readonly DiscountProtoService.DiscountProtoServiceClient _discountProtoServiceClient;

        public BasketService(
            IBasketRepository repository,
            IMapper mapper,
            ILogger<BasketService> logger,
            DiscountProtoService.DiscountProtoServiceClient discountProtoServiceClient)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _discountProtoServiceClient = discountProtoServiceClient ?? throw new ArgumentNullException(nameof(discountProtoServiceClient));
        }

        public override async Task<ShoppingCartModel> GetBasket(GetBasketRequest request, ServerCallContext context)
        {
            var basket = await _repository.GetBasketAsync(request.UserName);
            basket ??= new Entities.ShoppingCart(request.UserName);
            _logger.LogInformation("Basket is retrieved for Username : {Username}", request.UserName);

            var shoppingCartModel = _mapper.Map<ShoppingCartModel>(basket);
            return shoppingCartModel;
        }

        public override async Task<ShoppingCartModel> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
        {
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

        public override async Task<DeletetBasketResponse> DeletetBasket(DeletetBasketRequest request, ServerCallContext context)
        {
            await _repository.DeleteBasketAsync(request.UserName);
            return new DeletetBasketResponse();
        }
    }
}
