using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.EntityFrameworkCore;

namespace BTKETicaretSitesi.Services
{
    public interface IFavoriteProductService
    {
        Task AddFavoriteProduct(string userId, int productId);
        Task RemoveFavoriteProduct(string userId, int productId);
        Task<List<Product>> GetUserFavoriteProducts(string userId);
        Task<bool> IsProductFavorite(string userId, int productId);
    }

    public class FavoriteProductService : IFavoriteProductService
    {
        private readonly ApplicationDbContext _context;

        public FavoriteProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddFavoriteProduct(string userId, int productId)
        {
            // Zaten favori mi kontrolü
            var existingFavorite = await _context.FavoriteProducts
                .FirstOrDefaultAsync(fp => fp.UserId == userId && fp.ProductId == productId);

            if (existingFavorite != null)
            {
                return; // Zaten favorilerde
            }

            var favoriteProduct = new FavoriteProduct
            {
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.Now
            };

            _context.FavoriteProducts.Add(favoriteProduct);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFavoriteProduct(string userId, int productId)
        {
            var favoriteProduct = await _context.FavoriteProducts
                .FirstOrDefaultAsync(fp => fp.UserId == userId && fp.ProductId == productId);

            if (favoriteProduct != null)
            {
                _context.FavoriteProducts.Remove(favoriteProduct);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Product>> GetUserFavoriteProducts(string userId)
        {
            return await _context.FavoriteProducts
                .Where(fp => fp.UserId == userId)
                .Include(fp => fp.Product)
                    .ThenInclude(p => p.Images) // Eğer ürün kategorisi de gerekli ise
                .Select(fp => fp.Product)
                .ToListAsync();
        }

        public async Task<bool> IsProductFavorite(string userId, int productId)
        {
            return await _context.FavoriteProducts
                .AnyAsync(fp => fp.UserId == userId && fp.ProductId == productId);
        }
    }
}
