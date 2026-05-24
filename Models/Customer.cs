using System;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Represents a customer/client of the car wash business.
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        
        public string FirstName { get; set; } = string.Empty;
        
        public string LastName { get; set; } = string.Empty;
        
        /// <summary>
        /// Unique phone number - primary identifier for walk-ins
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Preferred payment method (0: Cash, 1: Card, 2: EFT)
        /// </summary>
        public int PreferredPaymentMethod { get; set; } = 0;
        
        /// <summary>
        /// Total amount spent across all jobs
        /// </summary>
        public decimal TotalSpent { get; set; } = 0;
        
        /// <summary>
        /// Loyalty/reward points
        /// </summary>
        public int LoyaltyPoints { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Full name for display purposes
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
