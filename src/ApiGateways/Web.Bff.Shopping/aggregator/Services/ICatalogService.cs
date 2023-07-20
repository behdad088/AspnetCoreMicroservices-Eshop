using Web.Shopping.HttpAggregator.Models;

namespace Web.Shopping.HttpAggregator.Services
{
    public interface ICatalogService
    {
        Task<IEnumerable<CatalogModel>> GetCatalogAsync();
        Task<IEnumerable<CatalogModel>> GetCatalogByCategoryAsync(string category);
        Task<CatalogModel> GetCatalogAsync(string id);
    }
}
