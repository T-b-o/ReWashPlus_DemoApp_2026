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
        Unknown    = 0,
        Waiting    = 1,
        InProgress = 2,
        Completed  = 3,
        Cancelled  = 4
    }

    /// <summary>
    /// Job type enumeration (Walk-in vs Pre-booked)
    /// </summary>
    public enum JobType
    {
        Unknown   = 0,
        WalkIn    = 1,
        PreBooked = 2
    }

    /// <summary>
    /// Represents a job/booking for a car wash.
    /// This combines both walk-in and pre-booked scenarios.
    /// </summary>
    public class Booking : IBranchEntity, IAuditableEntity
    {
        // ── Core Identifiers ─────────────────────────────────────────────────

        /// <summary>Legacy integer key — used by LocalStorage layer.</summary>
        public int    Id         { get; set; }

        /// <summary>GUID primary key used by the server and sync layer.</summary>
        public Guid   BookingId  { get; set; } = Guid.NewGuid();

        /// <summary>Human-readable job reference, e.g. JOB-20250120-001.</summary>
        public string JobNumber  { get; set; } = string.Empty;

        // ── Multi-tenant Scoping ──────────────────────────────────────────────

        public Guid TenantId { get; set; }
        public Guid BranchId { get; set; }

        // ── Customer & Vehicle Info ───────────────────────────────────────────

        public int    CustomerId    { get; set; }
        public string CustomerName  { get; set; } = string.Empty;
        public string PhoneNumber   { get; set; } = string.Empty;
        public string Email         { get; set; } = string.Empty;

        /// <summary>FK to Vehicle.VehicleId — preferred for all new bookings.</summary>
        public Guid   VehicleId     { get; set; }

        /// <summary>Legacy integer FK to Car.Id — kept for LocalStorage backward-compat.</summary>
        public int    CarId         { get; set; }

        public string CarPlate      { get; set; } = string.Empty;

        /// <summary>Deprecated free-text vehicle description. Use Vehicle.DisplayName going forward.</summary>
        [Obsolete("Populate VehicleId instead. This field exists only for migration compatibility.")]
        public string? CarDescription { get; set; }

        // ── Service Information ───────────────────────────────────────────────

        /// <summary>Legacy single-service field. Use the Services list for all new bookings.</summary>
        public string ServiceType { get; set; } = "Wash";

        public List<JobService> Services { get; set; } = new List<JobService>();

        // ── Status & Timeline ─────────────────────────────────────────────────

        public JobStatus Status        { get; set; } = JobStatus.Waiting;
        public JobType   Type          { get; set; } = JobType.WalkIn;
        public DateTime  AppointmentAt { get; set; } = DateTime.Today.AddDays(1);
        public DateTime? ArrivedAt     { get; set; }
        public DateTime? StartedAt     { get; set; }
        public DateTime? CompletedAt   { get; set; }

        // ── Financial Info ────────────────────────────────────────────────────

        public decimal  TotalAmount { get; set; } = 0;
        public Payment? Payment     { get; set; }

        // ── Staff Assignment ──────────────────────────────────────────────────

        /// <summary>FK to StaffProfile.StaffProfileId — preferred for all new bookings.</summary>
        public Guid?  AssignedStaffProfileId { get; set; }

        /// <summary>Legacy integer FK — kept for backward-compat with existing JobService code.</summary>
        public int?   AssignedStaffId        { get; set; }

        public string Notes { get; set; } = string.Empty;

        // ── Offline Sync ──────────────────────────────────────────────────────

        /// <summary>Offline synchronisation state.</summary>
        public SyncStatus SyncState { get; set; } = SyncStatus.Pending;

        /// <summary>
        /// Legacy sync flag — kept for backward-compat with existing service code.
        /// Setting IsSynced = true promotes SyncState to Synced automatically.
        /// </summary>
        public bool IsSynced
        {
            get => SyncState == SyncStatus.Synced;
            set { if (value) SyncState = SyncStatus.Synced; }
        }

        /// <summary>Optimistic-concurrency token; populated after first successful server sync.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // ── Audit ─────────────────────────────────────────────────────────────

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