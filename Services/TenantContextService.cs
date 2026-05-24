using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Holds the current tenant and branch context for the authenticated session.
    /// Populated from JWT claims on login (Sprint 7+).
    /// Falls back to a default dev tenant/branch so the app works before auth is wired.
    /// Persisted to LocalStorage so offline sessions retain their context.
    /// </summary>
    public class TenantContextService
    {
        private const string TenantIdKey     = "rw_tenant_id";
        private const string BranchIdKey     = "rw_branch_id";
        private const string BranchNameKey   = "rw_branch_name";
        private const string CompanyNameKey  = "rw_company_name";

        // Development defaults — replaced by real tenant data post-login
        public static readonly Guid DefaultTenantId = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid DefaultBranchId = new("00000000-0000-0000-0000-000000000002");

        private readonly ILocalStorageService _localStorage;

        public Guid   TenantId    { get; private set; } = DefaultTenantId;
        public Guid   BranchId    { get; private set; } = DefaultBranchId;
        public string BranchName  { get; private set; } = "Main Branch";
        public string CompanyName { get; private set; } = "RewashPlus";

        public TenantContextService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        /// <summary>
        /// Load persisted context from LocalStorage (call once on app startup).
        /// </summary>
        public async Task InitializeAsync()
        {
            var tenantId    = await _localStorage.GetItemAsync<Guid?>(TenantIdKey);
            var branchId    = await _localStorage.GetItemAsync<Guid?>(BranchIdKey);
            var branchName  = await _localStorage.GetItemAsync<string?>(BranchNameKey);
            var companyName = await _localStorage.GetItemAsync<string?>(CompanyNameKey);

            TenantId    = tenantId    ?? DefaultTenantId;
            BranchId    = branchId    ?? DefaultBranchId;
            BranchName  = branchName  ?? "Main Branch";
            CompanyName = companyName ?? "RewashPlus";
        }

        /// <summary>
        /// Update and persist the tenant/branch context (called after login or branch switch).
        /// </summary>
        public async Task SetContextAsync(
            Guid   tenantId,
            Guid   branchId,
            string branchName,
            string companyName)
        {
            TenantId    = tenantId;
            BranchId    = branchId;
            BranchName  = branchName;
            CompanyName = companyName;

            await _localStorage.SetItemAsync(TenantIdKey,    tenantId);
            await _localStorage.SetItemAsync(BranchIdKey,    branchId);
            await _localStorage.SetItemAsync(BranchNameKey,  branchName);
            await _localStorage.SetItemAsync(CompanyNameKey, companyName);
        }

        /// <summary>
        /// Convenience: applies TenantId and BranchId to any entity that implements IBranchEntity.
        /// </summary>
        public void ApplyContext(IBranchEntity entity)
        {
            entity.TenantId = TenantId;
            entity.BranchId = BranchId;
        }

        /// <summary>
        /// Convenience: applies TenantId to any entity that implements ITenantEntity.
        /// </summary>
        public void ApplyContext(ITenantEntity entity)
        {
            entity.TenantId = TenantId;
        }
    }
}
