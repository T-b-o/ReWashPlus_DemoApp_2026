namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Well-known feature toggle codes.
    /// Use these constants everywhere — never hard-code the string inline.
    /// </summary>
    public static class FeatureCodes
    {
        /// <summary>Washer travels to the customer's location.</summary>
        public const string MobileWash    = "MobileWash";

        /// <summary>Staff picks up / drops off the customer's vehicle.</summary>
        public const string PickupDropoff = "PickupDropoff";

        /// <summary>Points-based loyalty rewards programme.</summary>
        public const string Loyalty       = "Loyalty";

        /// <summary>Stock / consumables inventory tracking.</summary>
        public const string Inventory     = "Inventory";

        /// <summary>Corporate / fleet account billing.</summary>
        public const string FleetAccounts = "FleetAccounts";

        /// <summary>Live queue display screen (TV / kiosk mode).</summary>
        public const string QueueDisplay  = "QueueDisplay";
    }
}
