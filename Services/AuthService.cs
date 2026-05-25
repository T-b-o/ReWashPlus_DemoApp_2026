using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Offline-first authentication service.
    ///
    /// All user accounts, tenants, and the active session are stored in browser
    /// LocalStorage so the app can register and log in with no network connection.
    ///
    /// Password security: SHA-256( password + randomSalt ) — good enough for a demo
    /// PWA; production would use bcrypt / Argon2 server-side.
    /// </summary>
    public class AuthService
    {
        private const string UsersKey    = "rw_auth_users";
        private const string TenantsKey  = "rw_tenants";
        private const string BranchesKey = "rw_branches";
        private const string SessionKey  = "rw_session";

        private readonly ILocalStorageService _localStorage;
        private readonly TenantContextService _tenantContext;

        // ── Public state ──────────────────────────────────────────────────────

        public AuthSession? CurrentUser    { get; private set; }
        public bool         IsAuthenticated => CurrentUser is not null;

        /// <summary>Fired whenever the auth state changes (login / logout).</summary>
        public event Action? AuthStateChanged;

        // ── Constructor ───────────────────────────────────────────────────────

        public AuthService(ILocalStorageService localStorage, TenantContextService tenantContext)
        {
            _localStorage  = localStorage;
            _tenantContext = tenantContext;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Restores the session from LocalStorage on app startup.
        /// Safe to call multiple times.
        /// </summary>
        public async Task InitializeAsync()
        {
            var session = await _localStorage.GetItemAsync<AuthSession?>(SessionKey);
            if (session is null) return;

            CurrentUser = session;

            var branches = await _localStorage.GetItemAsync<List<Branch>>(BranchesKey) ?? new();
            var branch   = branches.FirstOrDefault(b => b.TenantId == session.TenantId);

            await _tenantContext.SetContextAsync(
                session.TenantId,
                branch?.BranchId ?? TenantContextService.DefaultBranchId,
                branch?.Name     ?? "Main Branch",
                session.CompanyName);
        }

        // ── Registration ──────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new tenant (business), its default branch, and the owner admin account.
        /// Auto-logs in the new admin and sets the tenant context.
        /// </summary>
        public async Task<(bool Success, string Error)> RegisterTenantAsync(
            string companyName,
            string branchName,
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string password)
        {
            var users = await _localStorage.GetItemAsync<List<AuthUser>>(UsersKey) ?? new();
            if (users.Any(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
                return (false, "An account with this email already exists.");

            var tenant = new Tenant
            {
                TenantId    = Guid.NewGuid(),
                CompanyName = companyName.Trim(),
                Slug        = SlugFrom(companyName),
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow
            };

            var branch = new Branch
            {
                BranchId  = Guid.NewGuid(),
                TenantId  = tenant.TenantId,
                Name      = string.IsNullOrWhiteSpace(branchName) ? "Main Branch" : branchName.Trim(),
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            };

            var (hash, salt) = HashPassword(password);
            var admin = new AuthUser
            {
                UserId       = Guid.NewGuid(),
                TenantId     = tenant.TenantId,
                Email        = email.Trim().ToLowerInvariant(),
                PasswordHash = hash,
                Salt         = salt,
                FirstName    = firstName.Trim(),
                LastName     = lastName.Trim(),
                PhoneNumber  = phoneNumber.Trim(),
                Role         = UserRole.TenantAdmin,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };

            // Persist all three records
            var tenants  = await _localStorage.GetItemAsync<List<Tenant>>(TenantsKey)   ?? new();
            var branches = await _localStorage.GetItemAsync<List<Branch>>(BranchesKey)  ?? new();

            tenants.Add(tenant);
            branches.Add(branch);
            users.Add(admin);

            await _localStorage.SetItemAsync(TenantsKey,  tenants);
            await _localStorage.SetItemAsync(BranchesKey, branches);
            await _localStorage.SetItemAsync(UsersKey,    users);

            // Auto-login
            await CreateSessionAsync(admin, tenant.CompanyName);
            await _tenantContext.SetContextAsync(
                tenant.TenantId,
                branch.BranchId,
                branch.Name,
                tenant.CompanyName);

            return (true, string.Empty);
        }

        /// <summary>
        /// Registers a new user under an existing tenant identified by its company slug.
        /// If no slug is provided the user is registered under the current tenant context.
        /// </summary>
        public async Task<(bool Success, string Error)> RegisterUserAsync(
            string   firstName,
            string   lastName,
            string   email,
            string   phoneNumber,
            string   password,
            UserRole role,
            string   companySlug = "")
        {
            var users = await _localStorage.GetItemAsync<List<AuthUser>>(UsersKey) ?? new();
            if (users.Any(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
                return (false, "An account with this email already exists.");

            // Resolve tenant
            Guid tenantId;
            if (!string.IsNullOrWhiteSpace(companySlug))
            {
                var tenants = await _localStorage.GetItemAsync<List<Tenant>>(TenantsKey) ?? new();
                var tenant  = tenants.FirstOrDefault(t =>
                    t.Slug.Equals(companySlug.Trim(), StringComparison.OrdinalIgnoreCase));
                if (tenant is null)
                    return (false, "Company code not found. Ask your manager for the correct code.");
                tenantId = tenant.TenantId;
            }
            else
            {
                tenantId = _tenantContext.TenantId;
            }

            var (hash, salt) = HashPassword(password);
            var newUser = new AuthUser
            {
                UserId       = Guid.NewGuid(),
                TenantId     = tenantId,
                Email        = email.Trim().ToLowerInvariant(),
                PasswordHash = hash,
                Salt         = salt,
                FirstName    = firstName.Trim(),
                LastName     = lastName.Trim(),
                PhoneNumber  = phoneNumber.Trim(),
                Role         = role,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };

            users.Add(newUser);
            await _localStorage.SetItemAsync(UsersKey, users);

            return (true, string.Empty);
        }

        // ── Login / Logout ────────────────────────────────────────────────────

        public async Task<(bool Success, string Error)> LoginAsync(string email, string password)
        {
            var users = await _localStorage.GetItemAsync<List<AuthUser>>(UsersKey) ?? new();
            var user  = users.FirstOrDefault(u =>
                u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.IsActive);

            if (user is null || !VerifyPassword(password, user.Salt, user.PasswordHash))
                return (false, "Invalid email or password.");

            var tenants     = await _localStorage.GetItemAsync<List<Tenant>>(TenantsKey)   ?? new();
            var branches    = await _localStorage.GetItemAsync<List<Branch>>(BranchesKey)  ?? new();
            var tenant      = tenants.FirstOrDefault(t => t.TenantId == user.TenantId);
            var branch      = branches.FirstOrDefault(b => b.TenantId == user.TenantId);
            var companyName = tenant?.CompanyName ?? string.Empty;

            await CreateSessionAsync(user, companyName);
            await _tenantContext.SetContextAsync(
                user.TenantId,
                branch?.BranchId ?? TenantContextService.DefaultBranchId,
                branch?.Name     ?? "Main Branch",
                companyName);

            return (true, string.Empty);
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync(SessionKey);
            CurrentUser = null;
            AuthStateChanged?.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task CreateSessionAsync(AuthUser user, string companyName)
        {
            var session = new AuthSession
            {
                UserId      = user.UserId,
                TenantId    = user.TenantId,
                Email       = user.Email,
                FullName    = user.FullName,
                CompanyName = companyName,
                Role        = user.Role
            };
            await _localStorage.SetItemAsync(SessionKey, session);
            CurrentUser = session;
            AuthStateChanged?.Invoke();
        }

        private static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt      = Convert.ToBase64String(saltBytes);
            var hash      = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
            return (hash, salt);
        }

        private static bool VerifyPassword(string password, string salt, string expectedHash)
        {
            var hash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
            return hash == expectedHash;
        }

        private static string SlugFrom(string name) =>
            name.Trim().ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("&", "and");
    }
}
