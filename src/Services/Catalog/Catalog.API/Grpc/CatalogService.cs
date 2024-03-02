using AutoMapper;
using Catalog.API.Entities;
using Catalog.API.Repositories;
using Catalog.Grpc.Protos;
using Grpc.Core;

namespace Catalog.API.Grpc
{
    public class CatalogService : CatalogProtoService.CatalogProtoServiceBase
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<CatalogService> _logger;
        private readonly IMapper _mapper;

        public CatalogService(IProductRepository repository, ILogger<CatalogService> logger, IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public override async Task<GetProductsResponse> GetProducts(NoRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Getting the list of all the products.");
            var products = await _repository.GetProductsAsync();
            var productModels = _mapper.Map<List<ProductModel>>(products);
            var result = new GetProductsResponse();
            result.Products.Add(productModels);
            return result;
        }

        public override async Task<ProductModel> GetProductById(GetProductByIdRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Getting product with Id={ProductId}.", request.Id);

            var product = await _repository.GetProductAsync(request.Id);

            if (product == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Product with ProductId={request.Id} is not found."));

            var productModel = _mapper.Map<ProductModel>(product);
            return productModel;
        }

        public override async Task<GetProductByCategoryResponse> GetProductByCategory(GetProductByCategoryRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Getting the list of all the products for Category={category}", request.Category);

            var products = await _repository.GetProductByCategoryAsync(request.Category);
            var productModels = _mapper.Map<List<ProductModel>>(products);
            var result = new GetProductByCategoryResponse();
            result.Products.Add(productModels);
            return result;
        }

        public override async Task<ProductModel> CreateProduct(CreateProductRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Cearting product: {request.Product}");
            var product = _mapper.Map<Product>(request.Product);
            await _repository.CreateProductAsync(product);

            var addedProduct = await GetProductById(new GetProductByIdRequest { Id = product.Id }, context);
            return addedProduct;
        }

        public override async Task<UpdateProductResponse> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Updating product {request.Product}");
            var product = _mapper.Map<Product>(request.Product);
            var result = await _repository.UpdateProductAsync(product);
            return new UpdateProductResponse() { Result = result };
        }

        public override async Task<DeleteProductIdResponse> DeleteProductById(DeleteProductIdRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Deleting product with Id {request.Id}");
            var result = await _repository.DeleteProductAsync(request.Id);
            return new DeleteProductIdResponse() { Result = result };
        }
    }
}
