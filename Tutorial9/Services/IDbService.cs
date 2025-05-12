using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task DoSomethingAsync();
    Task ProcedureAsync();
    public Task<bool> AddProductAsync(AddProductDTO product);
    // public Task<bool> AddProductUsingProcedureAsync(AddProductDTO product);
}