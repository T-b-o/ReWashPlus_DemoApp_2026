using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Manages vehicles in offline-first mode.
    /// Replaces CarService — uses the Vehicle model (renamed from Car) and
    /// scopes all operations to the current tenant via TenantContextService.
    /// </summary>
    public class VehicleService
    {
        private const string VehiclesKey = "rw_vehicles";
        private const string NextIdKey   = "rw_vehicle_next_id";

        private readonly ILocalStorageService _localStorage;
        private readonly TenantContextService _tenantContext;
        private List<Vehicle>? _cachedVehicles;
        private int _nextId = 1;

        public VehicleService(
            ILocalStorageService localStorage,
            TenantContextService tenantContext)
        {
            _localStorage  = localStorage;
            _tenantContext = tenantContext;
        }

        public async Task InitializeAsync()
        {
            _cachedVehicles = await _localStorage.GetItemAsync<List<Vehicle>>(VehiclesKey)
                              ?? new List<Vehicle>();

            _nextId = await _localStorage.GetItemAsync<int>(NextIdKey);
            if (_nextId == 0)
                _nextId = (_cachedVehicles.Count) + 1;
        }

        // ── Read ──────────────────────────────────────────────────────────────

        public async Task<List<Vehicle>> GetAllAsync()
        {
            await EnsureInitializedAsync();
            return _cachedVehicles!
                .Where(v => v.TenantId == _tenantContext.TenantId && v.IsActive)
                .ToList();
        }

        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            return _cachedVehicles?.FirstOrDefault(
                v => v.Id == id &&
                     v.TenantId == _tenantContext.TenantId &&
                     v.IsActive);
        }

        public async Task<Vehicle?> GetByVehicleIdAsync(Guid vehicleId)
        {
            await EnsureInitializedAsync();
            return _cachedVehicles?.FirstOrDefault(
                v => v.VehicleId == vehicleId &&
                     v.TenantId  == _tenantContext.TenantId &&
                     v.IsActive);
        }

        public async Task<List<Vehicle>> GetByCustomerIdAsync(int customerId)
        {
            await EnsureInitializedAsync();
            return _cachedVehicles?
                .Where(v =>
                    v.CustomerId == customerId &&
                    v.TenantId   == _tenantContext.TenantId &&
                    v.IsActive)
                .ToList() ?? new List<Vehicle>();
        }

        public async Task<Vehicle?> GetByRegistrationAsync(string registrationNumber)
        {
            await EnsureInitializedAsync();
            return _cachedVehicles?.FirstOrDefault(v =>
                v.RegistrationNumber.Equals(
                    registrationNumber,
                    StringComparison.OrdinalIgnoreCase) &&
                v.TenantId == _tenantContext.TenantId &&
                v.IsActive);
        }

        // ── Write ─────────────────────────────────────────────────────────────

        public async Task<Vehicle> CreateAsync(Vehicle vehicle)
        {
            await EnsureInitializedAsync();

            if (await GetByRegistrationAsync(vehicle.RegistrationNumber) != null)
                throw new InvalidOperationException(
                    $"A vehicle with registration '{vehicle.RegistrationNumber}' already exists.");

            _tenantContext.ApplyContext(vehicle);

            vehicle.Id         = _nextId++;
            vehicle.VehicleId  = Guid.NewGuid();
            vehicle.SyncState  = SyncStatus.Pending;
            vehicle.CreatedAt  = DateTime.UtcNow;
            vehicle.UpdatedAt  = DateTime.UtcNow;

            _cachedVehicles!.Add(vehicle);
            await PersistAsync();

            return vehicle;
        }

        public async Task<Vehicle?> UpdateAsync(Vehicle vehicle)
        {
            await EnsureInitializedAsync();

            var existing = _cachedVehicles?.FirstOrDefault(v =>
                v.Id == vehicle.Id &&
                v.TenantId == _tenantContext.TenantId);
            if (existing is null) return null;

            existing.CustomerId         = vehicle.CustomerId;
            existing.RegistrationNumber = vehicle.RegistrationNumber;
            existing.Make               = vehicle.Make;
            existing.Model              = vehicle.Model;
            existing.Year               = vehicle.Year;
            existing.Color              = vehicle.Color;
            existing.VinNumber          = vehicle.VinNumber;
            existing.Category           = vehicle.Category;
            existing.SyncState          = SyncStatus.Pending;
            existing.UpdatedAt          = DateTime.UtcNow;

            await PersistAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await EnsureInitializedAsync();

            var vehicle = _cachedVehicles?.FirstOrDefault(v =>
                v.Id == id &&
                v.TenantId == _tenantContext.TenantId);
            if (vehicle is null) return false;

            vehicle.IsActive   = false;
            vehicle.SyncState  = SyncStatus.Pending;
            vehicle.UpdatedAt  = DateTime.UtcNow;
            await PersistAsync();

            return true;
        }

        public async Task ClearAllAsync()
        {
            _cachedVehicles = new List<Vehicle>();
            _nextId = 1;
            await PersistAsync();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private async Task PersistAsync()
        {
            await _localStorage.SetItemAsync(VehiclesKey, _cachedVehicles);
            await _localStorage.SetItemAsync(NextIdKey,   _nextId);
        }

        private async Task EnsureInitializedAsync()
        {
            if (_cachedVehicles is null)
                await InitializeAsync();
        }
    }
}
