using System;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Payment method enumeration
    /// </summary>
    public enum PaymentMethod
    {
        Unknown = 0,
        Cash = 1,
        Card = 2,
        EFT = 3
    }

    /// <summary>
    /// Payment status enumeration
    /// </summary>
    public enum PaymentStatus
    {
        Unknown = 0,
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Refunded = 4
    }

    /// <summary>
    /// Represents a payment transaction for a completed job.
    /// </summary>
    public class Payment
    {
        public int Id { get; set; }
        
        public int JobId { get; set; }
        
        /// <summary>
        /// Total amount paid in ZAR
        /// </summary>
        public decimal Amount { get; set; } = 0;
        
        /// <summary>
        /// Payment method (Cash, Card, EFT)
        /// </summary>
        public PaymentMethod Method { get; set; } = PaymentMethod.Unknown;
        
        /// <summary>
        /// Payment status (Pending, Completed, Failed)
        /// </summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        
        /// <summary>
        /// Transaction reference (e.g., card last 4 digits, card ref, bank transfer ID)
        /// </summary>
        public string Reference { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp when payment was completed
        /// </summary>
        public DateTime? PaidAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether payment has been successfully processed
        /// </summary>
        public bool IsPaid => Status == PaymentStatus.Completed && PaidAt.HasValue;
    }
}
