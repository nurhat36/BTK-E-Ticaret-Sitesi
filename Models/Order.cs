namespace BTKETicaretSitesi.Models
{
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Refunded
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } // Benzersiz sipariş numarası
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal OrderTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TaxAmount { get; set; }

        // Müşteri bilgileri
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }
        public string ShippingAddress { get; set; }
        public string BillingAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        // Ödeme bilgileri
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string TransactionId { get; set; }

        public ICollection<OrderItem> Items { get; set; }
        public ICollection<OrderStatusHistory> StatusHistory { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int? VariantId { get; set; }
        public ProductVariant Variant { get; set; }

        public int StoreId { get; set; }
        public Store Store { get; set; }
    }

    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public string Notes { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string ChangedByUserId { get; set; }
        public ApplicationUser ChangedByUser { get; set; }
    }
}