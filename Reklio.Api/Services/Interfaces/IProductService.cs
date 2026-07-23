using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(int id);
}