namespace BTKETicaretSitesi.Models.ViewModels
{
    public class ProductSearchViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string ImageUrl { get; set; }
        public string StoreName { get; set; }
        public string StoreSlug { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

    public class ProductSearchResultViewModel
    {
        public string Query { get; set; }
        public int? CategoryId { get; set; }
        public List<ProductSearchViewModel> Products { get; set; }
        public PaginationViewModel Pagination { get; set; }
    }

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage);
    }
}
