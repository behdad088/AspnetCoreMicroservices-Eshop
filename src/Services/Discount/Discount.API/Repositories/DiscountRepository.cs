using Dapper;
using Discount.API.Entities;
using Npgsql;

namespace Discount.API.Repositories
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly string _connectionString;

        public DiscountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> CreateDiscountAsync(Coupon coupon)
        {
            using var connection = GetNpgsqlConnection(_connectionString);

            var affected =
                await connection.ExecuteAsync
                    ("INSERT INTO Coupon (ProductName, Description, Amount) VALUES (@ProductName, @Description, @Amount)",
                            new { ProductName = coupon.ProductName, Description = coupon.Description, Amount = coupon.Amount });

            return affected != 0;
        }

        public async Task<bool> DeleteDiscountAsync(string productName)
        {
            using var connection = GetNpgsqlConnection(_connectionString);

            var affected = await connection.ExecuteAsync("DELETE FROM Coupon WHERE ProductName = @ProductName",
                new { ProductName = productName });

            return affected != 0;
        }

        public async Task<Coupon> GetDiscountAsync(string productName)
        {
            using var connection = GetNpgsqlConnection(_connectionString);

            var coupon = await connection.QueryFirstOrDefaultAsync<Coupon>
                ("SELECT * FROM Coupon WHERE ProductName = @ProductName", new { ProductName = productName });

            if (coupon == null)
                return new Coupon
                { ProductName = "No Discount", Amount = 0, Description = "No Discount Desc" };

            return coupon;
        }

        public async Task<bool> UpdateDiscountAsync(Coupon coupon)
        {
            using var connection = GetNpgsqlConnection(_connectionString);

            var affected = await connection.ExecuteAsync
                    ("UPDATE Coupon SET ProductName=@ProductName, Description = @Description, Amount = @Amount WHERE Id = @Id",
                            new { ProductName = coupon.ProductName, Description = coupon.Description, Amount = coupon.Amount, Id = coupon.Id });

            return affected != 0;
        }

        private NpgsqlConnection GetNpgsqlConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("connection string cannot be null or empty.");

            return new NpgsqlConnection(connectionString);
        }
    }
}
