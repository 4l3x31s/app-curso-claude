namespace app_curso_claude.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public DateTime PurchasedAt { get; set; }

        public Customer Customer { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
