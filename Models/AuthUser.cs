namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Role assigned to every authenticated user account.
    /// </summary>
    public enum UserRole
    {
        Customer      = 0,
        Staff         = 1,
        BranchManager = 2,
        TenantAdmin   = 3,
        SuperAdmin    = 4
    }

    /// <summary>
    /// Authentication record stored offline in the browser's LocalStorage.
    /// Passwords are hashed with SHA-256 + random salt — never stored in plain text.
    /// In production this record would live server-side; here it powers fully offline login.
    /// </summary>
    public class AuthUser
    {
        public Guid     UserId       { get; set; } = Guid.NewGuid();
        public Guid     TenantId     { get; set; }

        public string   Email        { get; set; } = string.Empty;

        /// <summary>SHA-256( password + Salt ) stored as Base64.</summary>
        public string   PasswordHash { get; set; } = string.Empty;

        /// <summary>Random 16-byte salt stored as Base64.</summary>
        public string   Salt         { get; set; } = string.Empty;

        public string   FirstName    { get; set; } = string.Empty;
        public string   LastName     { get; set; } = string.Empty;
        public string   PhoneNumber  { get; set; } = string.Empty;
        public UserRole Role         { get; set; } = UserRole.Customer;
        public bool     IsActive     { get; set; } = true;
        public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// Lightweight session record written to LocalStorage after a successful login.
    /// Cleared on logout. Loaded on app start to restore auth state without re-login.
    /// </summary>
    public class AuthSession
    {
        public Guid     UserId      { get; set; }
        public Guid     TenantId    { get; set; }
        public string   Email       { get; set; } = string.Empty;
        public string   FullName    { get; set; } = string.Empty;
        public string   PhoneNumber { get; set; } = string.Empty;
        public string   CompanyName { get; set; } = string.Empty;
        public UserRole Role        { get; set; }
    }
}
