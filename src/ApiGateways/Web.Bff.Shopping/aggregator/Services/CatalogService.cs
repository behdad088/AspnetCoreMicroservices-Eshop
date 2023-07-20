using AutoMapper;
using Catalog.Grpc.Protos;
using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly CatalogProtoService.CatalogProtoServiceClient _cataloServiceClient;
        private readonly ILogger<CatalogService> _logger;
        private readonly IMapper _mapper;

        public CatalogService(CatalogProtoService.CatalogProtoServiceClient cataloServiceClient, ILogger<CatalogService> logger, IMapper mapper)
        {
            _cataloServiceClient = cataloServiceClient ?? throw new ArgumentNullException(nameof(cataloServiceClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<IEnumerable<CatalogModel>> GetCatalogAsync()
        {
            var catalogs = await _cataloServiceClient.GetProductsAsync(new NoRequest());
            var result = _mapper.Map<List<CatalogModel>>(catalogs.Products);
            return result;
        }

        public async Task<CatalogModel> GetCatalogAsync(string id)
        {
            var catalog = await _cataloServiceClient.GetProductByIdAsync(new GetProductByIdRequest() { Id = id });
            var result = _mapper.Map<CatalogModel>(catalog);
            return result;
        }

        public async Task<IEnumerable<CatalogModel>> GetCatalogByCategoryAsync(string category)
        {
            var catalogs = await _cataloServiceClient.GetProductByCategoryAsync(new GetProductByCategoryRequest() { Category = category });
            var result = _mapper.Map<List<CatalogModel>>(catalogs.Products);
            return result;
        }
    }
}
