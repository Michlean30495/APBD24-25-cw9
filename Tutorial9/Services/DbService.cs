using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task DoSomethingAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@Name", "Animal1");
        
            await command.ExecuteNonQueryAsync();
        
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 2);
            command.Parameters.AddWithValue("@Name", "Animal2");
        
            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task ProcedureAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "NazwaProcedury";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@Id", 2);
        
        await command.ExecuteNonQueryAsync();
        
    }

    public async Task<bool> AddProductAsync(AddProductDTO product)
    {
        using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var querry = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            var command = new SqlCommand(querry, connection, transaction);
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            var result = await command.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                throw new Exception("Product not found");
            
            var productPrice = Convert.ToDecimal(result);
            if (productPrice < 0)
                throw new Exception("Price must be greater than 0");
            
            var querry2 = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            var command2 = new SqlCommand(querry2, connection, transaction);
            command2.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
            var result2 = await command2.ExecuteScalarAsync();
            if (result2 == null)
                throw new Exception("Warehouse not found");

            var query3 = @"
                SELECT MAX(o.IdOrder) FROM [Order] o
                WHERE o.IdProduct = @IdProduct
                  AND o.Amount = @Amount
                  AND o.CreatedAt < @CreatedAt
                  AND NOT EXISTS (
                      SELECT 1 FROM Product_Warehouse pw WHERE pw.IdOrder = o.IdOrder
                  )
            ";
            var command3 = new SqlCommand(query3, connection, transaction);
            command3.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            command3.Parameters.AddWithValue("@Amount", product.Amount);
            command3.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);
            var result3 = await command3.ExecuteScalarAsync();
            if (result3 == null || result3 == DBNull.Value)
                throw new Exception("no matching order or already done");
            
            var orderId = Convert.ToInt32(result3);

            var query4 = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
            var command4 = new SqlCommand(query4, connection, transaction);
            command4.Parameters.AddWithValue("@IdOrder", orderId);
            command4.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);
            await command4.ExecuteScalarAsync();

            var finalQuery = @"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
            ";
            command4 = new SqlCommand(finalQuery, connection, transaction);
            command4.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
            command4.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            command4.Parameters.AddWithValue("@Amount", product.Amount);
            command4.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);
            command4.Parameters.AddWithValue("@IdOrder", orderId);
            command4.Parameters.AddWithValue("@Price", productPrice * product.Amount);
            await command4.ExecuteScalarAsync();
            
            transaction.Commit();
            return true;
        }   
        catch (Exception e)
        {
            Console.WriteLine("Błąd: " + e.Message); 
            transaction.Rollback();
            return false;
        }
    }
}