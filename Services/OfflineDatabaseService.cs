using Microsoft.JSInterop;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Wraps the browser's IndexedDB via JS interop.
    /// Provides typed, async CRUD operations for offline data storage.
    /// Use <see cref="StoreNames"/> constants for storeName parameters.
    /// 
    /// Replaces the raw Blazored.LocalStorage usage in individual services.
    /// Migration path: switch services from LocalStorage to this service one at a time.
    /// </summary>
    public class OfflineDatabaseService
    {
        private readonly IJSRuntime _js;
        private bool _initialized;

        public OfflineDatabaseService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>Opens (or creates) the IndexedDB database and all object stores.</summary>
        public async Task InitializeAsync()
        {
            if (_initialized) return;
            await _js.InvokeVoidAsync("rewashDb.initialize");
            _initialized = true;
        }

        // ── Read ──────────────────────────────────────────────────────────────

        /// <summary>Returns all records from the named store.</summary>
        public async Task<List<T>> GetAllAsync<T>(string storeName)
        {
            await EnsureInitializedAsync();
            return await _js.InvokeAsync<List<T>>("rewashDb.getAll", storeName);
        }

        /// <summary>Returns a single record by its localId string key. Returns null if not found.</summary>
        public async Task<T?> GetByIdAsync<T>(string storeName, string localId)
        {
            await EnsureInitializedAsync();
            return await _js.InvokeAsync<T?>("rewashDb.getById", storeName, localId);
        }

        /// <summary>Returns all records whose syncStatus field equals "Pending".</summary>
        public async Task<List<T>> GetPendingAsync<T>(string storeName)
        {
            await EnsureInitializedAsync();
            return await _js.InvokeAsync<List<T>>("rewashDb.getPending", storeName);
        }

        /// <summary>Returns records with LastModified greater than the given UTC timestamp.</summary>
        public async Task<List<T>> GetModifiedSinceAsync<T>(string storeName, DateTimeOffset since)
        {
            await EnsureInitializedAsync();
            return await _js.InvokeAsync<List<T>>(
                "rewashDb.getModifiedSince",
                storeName,
                since.ToUnixTimeMilliseconds());
        }

        // ── Write ─────────────────────────────────────────────────────────────

        /// <summary>Inserts or updates a record. The record must have a <c>localId</c> property.</summary>
        public async Task UpsertAsync<T>(string storeName, T item)
        {
            await EnsureInitializedAsync();
            await _js.InvokeVoidAsync("rewashDb.upsert", storeName, item);
        }

        /// <summary>Batch upsert — more efficient than calling UpsertAsync in a loop.</summary>
        public async Task UpsertManyAsync<T>(string storeName, IEnumerable<T> items)
        {
            await EnsureInitializedAsync();
            await _js.InvokeVoidAsync("rewashDb.upsertMany", storeName, items);
        }

        /// <summary>Marks a record as logically deleted (sets isDeleted=true, syncStatus="Pending").</summary>
        public async Task SoftDeleteAsync(string storeName, string localId)
        {
            await EnsureInitializedAsync();
            await _js.InvokeVoidAsync("rewashDb.softDelete", storeName, localId);
        }

        // ── Sync State ────────────────────────────────────────────────────────

        /// <summary>
        /// Updates a record's sync metadata after a successful server upload.
        /// Sets serverId, rowVersion, syncStatus="Synced".
        /// </summary>
        public async Task MarkSyncedAsync(
            string storeName,
            string localId,
            string serverId,
            string rowVersionBase64)
        {
            await EnsureInitializedAsync();
            await _js.InvokeVoidAsync(
                "rewashDb.markSynced",
                storeName,
                localId,
                serverId,
                rowVersionBase64);
        }

        /// <summary>Marks a record as Conflict (server returned HTTP 409).</summary>
        public async Task MarkConflictAsync(string storeName, string localId)
        {
            await EnsureInitializedAsync();
            await _js.InvokeVoidAsync("rewashDb.markConflict", storeName, localId);
        }

        // ── Maintenance ───────────────────────────────────────────────────────

        /// <summary>Deletes all records from the named store. Use for full re-sync scenarios.</summary>
        public async Task ClearStoreAsync(string storeName)
        {
            await EnsureInitializedAsync();
            await _js.InvokeVoidAsync("rewashDb.clearStore", storeName);
        }

        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
                await InitializeAsync();
        }
    }

    /// <summary>IndexedDB object store name constants.</summary>
    public static class StoreNames
    {
        public const string Bookings  = "bookings";
        public const string Customers = "customers";
        public const string Vehicles  = "vehicles";
        public const string Payments  = "payments";
        public const string Services  = "services";
    }
}
