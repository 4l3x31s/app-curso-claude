namespace app_curso_claude.Models
{
    public class Customer
    {
        public int Id { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string Email { get; set; }

        public required string Phone { get; set; }

        public required string Address { get; set; }

        public required string City { get; set; }

        public required string Country { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}
