namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// A platform user account (identity record).
    /// Customers and staff are composed with AppUser via UserId — NOT via inheritance.
    /// </summary>
    public class AppUser
    {
        public Guid   UserId      { get; set; } = Guid.NewGuid();
        public string FirstName   { get; set; } = string.Empty;
        public string LastName    { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Firebase Cloud Messaging device token for push notifications.
        /// Stored here so notifications work without a full profile loaded.
        /// </summary>
        public string FcmToken    { get; set; } = string.Empty;

        public bool   IsActive    { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
