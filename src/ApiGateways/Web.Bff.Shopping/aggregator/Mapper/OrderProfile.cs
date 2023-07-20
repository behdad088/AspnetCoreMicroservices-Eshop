using AutoMapper;
using Order.Grpc.Protos;
using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Mapper
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<OrderResponseModel, OrderViewModel>().ReverseMap();
        }
    }
}
