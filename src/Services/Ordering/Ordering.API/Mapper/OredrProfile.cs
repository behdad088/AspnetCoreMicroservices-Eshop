using AutoMapper;
using Order.Grpc.Protos;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrderLists;

namespace Ordering.API.Mapper
{
    public class OredrProfile : Profile
    {
        public OredrProfile()
        {
            CreateMap<OrdersVm, OrderViewModel>().ReverseMap();
            CreateMap<UpdateOrderCommand, OrderViewModel>().ReverseMap();
            CreateMap<CheckoutOrderCommand, CheckoutViewModel>().ReverseMap();
        }
    }
}
