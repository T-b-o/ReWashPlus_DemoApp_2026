/**
 * rewashDb — IndexedDB wrapper for RewashPlus offline storage.
 *
 * Database : "rewashplus_db"  (version 1)
 * Stores   : bookings | customers | vehicles | payments | services
 *
 * Every record must have a "localId" (string GUID) as its keyPath.
 * Sync metadata fields stored on each record:
 *   syncStatus  : "Pending" | "Synced" | "Failed" | "Conflict"
 *   serverId    : string | null
 *   rowVersion  : string (base-64) | null
 *   lastModified: number (Unix ms timestamp)
 *   isDeleted   : boolean
 *
 * C# usage:
 *   await JS.InvokeVoidAsync("rewashDb.initialize");
 *   var list = await JS.InvokeAsync<List<T>>("rewashDb.getAll", "bookings");
 *   await JS.InvokeVoidAsync("rewashDb.upsert", "bookings", item);
 */
window.rewashDb = (function () {

    const DB_NAME    = 'rewashplus_db';
    const DB_VERSION = 1;
    const STORES     = ['bookings', 'customers', 'vehicles', 'payments', 'services'];

    let _db = null;

    // ── Helpers ────────────────────────────────────────────────────────────

    function openDb() {
        return new Promise((resolve, reject) => {
            if (_db) { resolve(_db); return; }

            const req = indexedDB.open(DB_NAME, DB_VERSION);

            req.onupgradeneeded = function (e) {
                const db = e.target.result;
                STORES.forEach(name => {
                    if (!db.objectStoreNames.contains(name)) {
                        const store = db.createObjectStore(name, { keyPath: 'localId' });
                        store.createIndex('syncStatus',   'syncStatus',   { unique: false });
                        store.createIndex('lastModified', 'lastModified', { unique: false });
                        store.createIndex('isDeleted',    'isDeleted',    { unique: false });
                    }
                });
            };

            req.onsuccess  = function (e) { _db = e.target.result; resolve(_db); };
            req.onerror    = function (e) { reject(e.target.error); };
        });
    }

    function tx(storeName, mode) {
        return _db.transaction([storeName], mode).objectStore(storeName);
    }

    function promisify(request) {
        return new Promise((resolve, reject) => {
            request.onsuccess = e => resolve(e.target.result);
            request.onerror   = e => reject(e.target.error);
        });
    }

    function getAllFromStore(store) {
        return new Promise((resolve, reject) => {
            const req = store.getAll();
            req.onsuccess = e => resolve(e.target.result);
            req.onerror   = e => reject(e.target.error);
        });
    }

    // ── Public API ─────────────────────────────────────────────────────────

    return {

        /** Open (or create) the database. Called once on app start. */
        initialize: async function () {
            await openDb();
        },

        /** Return all records in the store. */
        getAll: async function (storeName) {
            await openDb();
            return getAllFromStore(tx(storeName, 'readonly'));
        },

        /** Return a single record by localId, or undefined if not found. */
        getById: async function (storeName, localId) {
            await openDb();
            return promisify(tx(storeName, 'readonly').get(localId));
        },

        /** Insert or update a record (key = record.localId). */
        upsert: async function (storeName, item) {
            await openDb();
            // Ensure required sync metadata defaults
            if (!item.localId)      item.localId      = crypto.randomUUID();
            if (!item.syncStatus)   item.syncStatus   = 'Pending';
            if (item.isDeleted === undefined) item.isDeleted = false;
            item.lastModified = Date.now();
            return promisify(tx(storeName, 'readwrite').put(item));
        },

        /** Insert or update multiple records in one transaction. */
        upsertMany: async function (storeName, items) {
            await openDb();
            const store = _db.transaction([storeName], 'readwrite').objectStore(storeName);
            const now   = Date.now();
            return new Promise((resolve, reject) => {
                let i = 0;
                function putNext() {
                    if (i >= items.length) { resolve(); return; }
                    const item = items[i++];
                    if (!item.localId)    item.localId    = crypto.randomUUID();
                    if (!item.syncStatus) item.syncStatus = 'Pending';
                    if (item.isDeleted === undefined) item.isDeleted = false;
                    item.lastModified = now;
                    const req = store.put(item);
                    req.onsuccess = putNext;
                    req.onerror   = e => reject(e.target.error);
                }
                putNext();
            });
        },

        /** Soft-delete: marks record isDeleted=true and syncStatus="Pending". */
        softDelete: async function (storeName, localId) {
            await openDb();
            const store    = tx(storeName, 'readwrite');
            const existing = await promisify(store.get(localId));
            if (!existing) return;
            existing.isDeleted    = true;
            existing.syncStatus   = 'Pending';
            existing.lastModified = Date.now();
            return promisify(store.put(existing));
        },

        /** Return all records where syncStatus === "Pending". */
        getPending: async function (storeName) {
            await openDb();
            return promisify(
                tx(storeName, 'readonly')
                    .index('syncStatus')
                    .getAll(IDBKeyRange.only('Pending'))
            );
        },

        /** Return all records with lastModified > sinceMs (Unix timestamp, ms). */
        getModifiedSince: async function (storeName, sinceMs) {
            await openDb();
            return promisify(
                tx(storeName, 'readonly')
                    .index('lastModified')
                    .getAll(IDBKeyRange.lowerBound(sinceMs, true))
            );
        },

        /**
         * After a successful server upload, mark the record Synced and
         * update its serverId + rowVersion.
         */
        markSynced: async function (storeName, localId, serverId, rowVersionBase64) {
            await openDb();
            const store    = tx(storeName, 'readwrite');
            const existing = await promisify(store.get(localId));
            if (!existing) return;
            existing.syncStatus   = 'Synced';
            existing.serverId     = serverId;
            existing.rowVersion   = rowVersionBase64;
            existing.lastModified = Date.now();
            return promisify(store.put(existing));
        },

        /** Mark a record as Conflict (HTTP 409 received from server). */
        markConflict: async function (storeName, localId) {
            await openDb();
            const store    = tx(storeName, 'readwrite');
            const existing = await promisify(store.get(localId));
            if (!existing) return;
            existing.syncStatus   = 'Conflict';
            existing.lastModified = Date.now();
            return promisify(store.put(existing));
        },

        /** Remove every record in the named store (use before a full re-sync). */
        clearStore: async function (storeName) {
            await openDb();
            return promisify(tx(storeName, 'readwrite').clear());
        }
    };

}());
