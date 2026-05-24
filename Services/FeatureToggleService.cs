using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System.Net.Http.Json;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Manages which platform features are enabled for the current tenant.
    /// Features are loaded from the API when online and cached in LocalStorage for offline use.
    /// Use <see cref="FeatureCodes"/> constants — never hard-code feature code strings.
    /// </summary>
    public class FeatureToggleService
    {
        private const string FeaturesKey = "rw_tenant_features";

        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient           _http;
        private readonly TenantContextService _tenantContext;

        private List<TenantFeature> _features = new();

        public FeatureToggleService(
            ILocalStorageService localStorage,
            HttpClient           http,
            TenantContextService tenantContext)
        {
            _localStorage  = localStorage;
            _http          = http;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Load features from LocalStorage (offline fallback).
        /// Call LoadFromApiAsync() separately when online to refresh.
        /// </summary>
        public async Task InitializeAsync()
        {
            _features = await _localStorage.GetItemAsync<List<TenantFeature>>(FeaturesKey)
                        ?? GetDevelopmentDefaults();
        }

        /// <summary>Returns true if the named feature is enabled for the current tenant.</summary>
        public bool IsEnabled(string featureCode)
            => _features
               .FirstOrDefault(f =>
                   f.FeatureCode == featureCode &&
                   f.TenantId    == _tenantContext.TenantId)
               ?.IsEnabled ?? false;

        /// <summary>
        /// Fetch the latest feature list from the API and update the local cache.
        /// Safe to call when offline — silently keeps existing cached features.
        /// </summary>
        public async Task LoadFromApiAsync()
        {
            try
            {
                var features = await _http.GetFromJsonAsync<List<TenantFeature>>(
                    $"api/v1/tenants/{_tenantContext.TenantId}/features");

                if (features is not null)
                {
                    _features = features;
                    await _localStorage.SetItemAsync(FeaturesKey, _features);
                }
            }
            catch
            {
                // Stay on cached features; offline or API unavailable
            }
        }

        /// <summary>Enable or disable a feature locally (persisted to cache).</summary>
        public async Task SetFeatureAsync(string featureCode, bool isEnabled)
        {
            var existing = _features.FirstOrDefault(
                f => f.FeatureCode == featureCode &&
                     f.TenantId    == _tenantContext.TenantId);

            if (existing is not null)
            {
                existing.IsEnabled = isEnabled;
            }
            else
            {
                _features.Add(new TenantFeature
                {
                    TenantId    = _tenantContext.TenantId,
                    FeatureCode = featureCode,
                    IsEnabled   = isEnabled
                });
            }

            await _localStorage.SetItemAsync(FeaturesKey, _features);
        }

        /// <summary>
        /// Default feature set used in development before real tenant data is loaded.
        /// Loyalty and QueueDisplay on; all others off.
        /// </summary>
        private List<TenantFeature> GetDevelopmentDefaults() =>
            new()
            {
                Feature(FeatureCodes.Loyalty,       isEnabled: true),
                Feature(FeatureCodes.QueueDisplay,  isEnabled: true),
                Feature(FeatureCodes.MobileWash,    isEnabled: false),
                Feature(FeatureCodes.PickupDropoff, isEnabled: false),
                Feature(FeatureCodes.Inventory,     isEnabled: false),
                Feature(FeatureCodes.FleetAccounts, isEnabled: false),
            };

        private TenantFeature Feature(string code, bool isEnabled) =>
            new() { TenantId = _tenantContext.TenantId, FeatureCode = code, IsEnabled = isEnabled };
    }
}
