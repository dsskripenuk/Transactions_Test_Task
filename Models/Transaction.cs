using System.ComponentModel.DataAnnotations;

namespace Transactions_test_task.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public string TransactionId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [RegularExpression(@"^-?\d+(\.\d+)?,-?\d+(\.\d+)?$", ErrorMessage = "Invalid coordinates format.")]
        public string ClientLocation { get; set; }

        public string ClientTimezone { get; set; }
        public DateTime ClientTimestamp { get; set; }
        public DateTime ServerTimestamp { get; set; }
    }
}
