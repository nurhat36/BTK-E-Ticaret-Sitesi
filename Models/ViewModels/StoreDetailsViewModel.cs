using BTKETicaretSitesi.Models;
using System.Collections.Generic;

namespace BTKETicaretSitesi.Models.ViewModels
{
    public class StoreDetailsViewModel
    {
        public Store Store { get; set; }
        public List<Product> PopularProducts { get; set; }
        public int AllProductsCount { get; set; }
    }
}