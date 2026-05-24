using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System.Net.Http.Json;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Coordinates offline-to-online synchronisation across all entity types.
    /// Individual services own their CRUD operations; SyncService orchestrates when
    /// and in what order those operations are flushed to the server.
    ///
    /// Usage: Call SyncAllAsync() on app resume, on connectivity restored, or manually.
    /// </summary>
    public class SyncService
    {
        private const string LastSyncKey = "rw_last_sync_timestamp";

        private readonly JobService         _jobService;
        private readonly PaymentService     _paymentService;
        private readonly ConnectivityService _connectivity;
        private readonly ILocalStorageService _localStorage;

        /// <summary>Raised whenever sync status text changes. Bind to UI status bar.</summary>
        public event Action<string>? SyncStatusChanged;

        public SyncService(
            JobService          jobService,
            PaymentService      paymentService,
            ConnectivityService connectivity,
            ILocalStorageService localStorage)
        {
            _jobService    = jobService;
            _paymentService = paymentService;
            _connectivity  = connectivity;
            _localStorage  = localStorage;
        }

        /// <summary>
        /// Runs a full sync cycle: uploads all pending records for every entity type.
        /// No-op if offline.
        /// </summary>
        public async Task SyncAllAsync()
        {
            if (!_connectivity.IsOnline)
            {
                SyncStatusChanged?.Invoke("Offline — sync skipped");
                return;
            }

            SyncStatusChanged?.Invoke("Syncing…");

            try
            {
                await _jobService.SyncPendingAsync();
                await _paymentService.SyncPendingAsync();
                // CustomerService and VehicleService sync added in Sprint 4
                // when they are migrated to the IndexedDB layer.

                await _localStorage.SetItemAsync(LastSyncKey, DateTimeOffset.UtcNow);
                SyncStatusChanged?.Invoke("Sync complete");
            }
            catch (Exception ex)
            {
                SyncStatusChanged?.Invoke($"Sync failed: {ex.Message}");
            }
        }

        /// <summary>Returns when the last successful full sync completed, or null if never.</summary>
        public async Task<DateTimeOffset?> GetLastSyncTimestampAsync()
            => await _localStorage.GetItemAsync<DateTimeOffset?>(LastSyncKey);
    }
}
