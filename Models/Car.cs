using System;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Represents a vehicle belonging to a customer.
    /// </summary>
    public class Car
    {
        public int Id { get; set; }
        
        public int CustomerId { get; set; }
        
        /// <summary>
        /// License plate number - unique identifier
        /// </summary>
        public string Plate { get; set; } = string.Empty;
        
        /// <summary>
        /// Vehicle make/brand (Toyota, BMW, etc.)
        /// </summary>
        public string Make { get; set; } = string.Empty;
        
        /// <summary>
        /// Vehicle model (Corolla, X5, etc.)
        /// </summary>
        public string Model { get; set; } = string.Empty;
        
        /// <summary>
        /// Manufacturing year
        /// </summary>
        public int? Year { get; set; }
        
        /// <summary>
        /// Vehicle color
        /// </summary>
        public string Color { get; set; } = string.Empty;
        
        /// <summary>
        /// VIN - Vehicle Identification Number (optional)
        /// </summary>
        public string VinNumber { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display-friendly representation (e.g., "2020 Toyota Corolla - White")
        /// </summary>
        public string DisplayName => $"{Year} {Make} {Model} - {Color}".Trim();
    }
}
