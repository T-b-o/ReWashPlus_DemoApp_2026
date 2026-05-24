using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Job status enumeration
    /// </summary>
    public enum JobStatus
    {
        Unknown = 0,
        Waiting = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Job type enumeration (Walk-in vs Pre-booked)
    /// </summary>
    public enum JobType
    {
        Unknown = 0,
        WalkIn = 1,
        PreBooked = 2
    }

    /// <summary>
    /// Represents a job/booking for a car wash.
    /// This combines both walk-in and pre-booked scenarios.
    /// </summary>
    public class Booking
    {
        // Core Identifiers
        public int Id { get; set; }
        
        /// <summary>
        /// Unique job number (e.g., JOB-20250120-001)
        /// </summary>
        public string JobNumber { get; set; } = string.Empty;
        
        // Customer & Vehicle Info
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        public int CarId { get; set; }
        public string CarPlate { get; set; } = string.Empty;
        public string CarDescription { get; set; } = string.Empty;  // "2020 Toyota Corolla - White"
        
        // Service Information
        public string ServiceType { get; set; } = "Wash";  // Legacy field, use Services list instead
        public List<JobService> Services { get; set; } = new List<JobService>();
        
        // Status & Timeline
        public JobStatus Status { get; set; } = JobStatus.Waiting;
        public JobType Type { get; set; } = JobType.WalkIn;
        
        /// <summary>
        /// Date for pre-bookings
        /// </summary>
        public DateTime AppointmentAt { get; set; } = DateTime.Today.AddDays(1);
        
        /// <summary>
        /// When the customer/car arrived
        /// </summary>
        public DateTime? ArrivedAt { get; set; }
        
        /// <summary>
        /// When work started on the job
        /// </summary>
        public DateTime? StartedAt { get; set; }
        
        /// <summary>
        /// When work was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        // Financial Info
        /// <summary>
        /// Total amount for all services (calculated from Services)
        /// </summary>
        public decimal TotalAmount { get; set; } = 0;
        
        /// <summary>
        /// Payment information
        /// </summary>
        public Payment? Payment { get; set; }
        
        // Admin Info
        public int? AssignedStaffId { get; set; }
        public string Notes { get; set; } = string.Empty;
        
        // Sync & Audit
        public bool IsSynced { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Helper Properties
        
        /// <summary>
        /// Check if job is currently active (not completed or cancelled)
        /// </summary>
        public bool IsActive => Status != JobStatus.Completed && Status != JobStatus.Cancelled;
        
        /// <summary>
        /// Check if job is waiting to start
        /// </summary>
        public bool IsWaiting => Status == JobStatus.Waiting;
        
        /// <summary>
        /// Check if job is currently being worked on
        /// </summary>
        public bool IsInProgress => Status == JobStatus.InProgress;
        
        /// <summary>
        /// Check if job has been completed
        /// </summary>
        public bool IsCompleted => Status == JobStatus.Completed;
        
        /// <summary>
        /// Check if payment has been recorded
        /// </summary>
        public bool IsPaid => Payment?.IsPaid ?? false;
        
        /// <summary>
        /// Duration from arrival to completion (in minutes)
        /// </summary>
        public int? DurationMinutes
        {
            get
            {
                if (ArrivedAt.HasValue && CompletedAt.HasValue)
                {
                    return (int)(CompletedAt.Value - ArrivedAt.Value).TotalMinutes;
                }
                return null;
            }
        }

        /// <summary>
        /// Calculate total amount from services
        /// </summary>
        public void RecalculateTotal()
        {
            TotalAmount = Services?.Sum(s => s.Subtotal) ?? 0;
        }
    }
}