using AutoMapper;
using Catalog.Grpc.Protos;
using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Mapper
{
    public class CatalogProfile : Profile
    {
        public CatalogProfile()
        {
            CreateMap<CatalogModel, ProductModel>().ReverseMap();
        }
    }
}
