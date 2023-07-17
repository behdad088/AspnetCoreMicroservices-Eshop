using AutoMapper;
using Basket.API.Entities;
using Basket.Grpc.Protos;

namespace Basket.API.Mapper
{
    public class ShoppingCartProfile : Profile
    {
        public ShoppingCartProfile()
        {
            CreateMap<ShoppingCart, ShoppingCartModel>().ReverseMap();
            CreateMap<ShoppingCartItem, ShoppingCartItemModel>().ReverseMap();
            CreateMap<BasketCheckout, CheckoutViewModel>().ReverseMap();
        }
    }
}
