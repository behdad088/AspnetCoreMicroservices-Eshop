using AutoMapper;
using Basket.Grpc.Protos;
using Grpc.Core;
using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Services
{
    public class BasketService : IBasketService
    {
        private readonly BasketProtoService.BasketProtoServiceClient _basketServiceClient;
        private readonly IMapper _mapper;
        private readonly ILogger<BasketService> _logger;

        public BasketService(BasketProtoService.BasketProtoServiceClient basketServiceClient, IMapper mapper, ILogger<BasketService> logger)
        {
            _basketServiceClient = basketServiceClient ?? throw new ArgumentNullException(nameof(basketServiceClient));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BasketModel> GetBasketAsync(string username)
        {
            var basket = await _basketServiceClient.GetBasketAsync(new GetBasketRequest() { Username = username });
            if (basket == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Basket not found."));

            var result = _mapper.Map<BasketModel>(basket);
            return result;
        }
    }
}
