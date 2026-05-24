namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Branch-specific operational configuration.
    /// Each branch has exactly one BranchSettings record.
    /// </summary>
    public class BranchSettings
    {
        public Guid   BranchSettingsId      { get; set; } = Guid.NewGuid();
        public Guid   BranchId             { get; set; }

        /// <summary>Maximum number of bookings accepted per hour slot.</summary>
        public int    MaxBookingsPerHour    { get; set; } = 10;

        /// <summary>Mobile wash service radius in kilometres. 0 = not applicable.</summary>
        public double ServiceRadiusKm      { get; set; } = 0;

        public bool   SupportsPickupDropoff { get; set; } = false;
        public bool   QueueDisplayEnabled  { get; set; } = false;
        public bool   AllowWalkIns         { get; set; } = true;

        /// <summary>
        /// JSON object keyed by day-of-week ("Monday"..."Sunday") with open/close times.
        /// Example: { "Monday": { "open": "08:00", "close": "17:00" } }
        /// </summary>
        public string OperatingHoursJson   { get; set; } = "{}";
    }
}
