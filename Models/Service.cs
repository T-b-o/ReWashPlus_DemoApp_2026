using System;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Represents a car wash service/offering with pricing.
    /// </summary>
    public class Service
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Service name (e.g., "Basic Wash", "Deluxe", "Interior Clean", "Full Valet")
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Detailed description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Price in ZAR (South African Rand)
        /// </summary>
        public decimal Price { get; set; } = 0;
        
        /// <summary>
        /// Estimated duration in minutes
        /// </summary>
        public int DurationMinutes { get; set; } = 30;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
