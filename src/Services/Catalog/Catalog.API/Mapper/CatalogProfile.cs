using AutoMapper;
using Catalog.API.Entities;
using Catalog.Grpc.Protos;

namespace Catalog.API.Mapper
{
    public class CatalogProfile : Profile
    {
        public CatalogProfile()
        {
            CreateMap<Product, ProductModel>().ReverseMap();
        }
    }
}
