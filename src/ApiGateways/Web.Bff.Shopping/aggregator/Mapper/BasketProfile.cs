using AutoMapper;
using Basket.Grpc.Protos;
using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Mapper
{
    public class BasketProfile : Profile
    {
        public BasketProfile()
        {
            CreateMap<BasketModel, ShoppingCartModel>().ReverseMap();
            CreateMap<BasketItemExtendedModel, ShoppingCartItemModel>().ReverseMap();
        }
    }
}
