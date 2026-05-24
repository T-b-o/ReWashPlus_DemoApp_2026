using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System.Net.Http.Json;

namespace ReWashPlus_DemoApp.Services
{
    public class BookingService
    {
        private const string PendingKey = "rw_pending_bookings";
        private const string SyncedKey = "rw_synced_bookings";
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _http;

        public BookingService(ILocalStorageService localStorage, HttpClient http)
        {
            _localStorage = localStorage;
            _http = http;
        }

        /// <summary>
        /// Save a booking locally (used by offline-first booking form).
        /// The booking will be queued for sync.
        /// </summary>
        public async Task SaveBookingAsync(Booking booking)
        {
            var pending = await _localStorage.GetItemAsync<List<Booking>>(PendingKey) ?? new List<Booking>();
            pending.Add(booking);
            await _localStorage.SetItemAsync(PendingKey, pending);
        }

        /// <summary>
        /// Returns the list of pending bookings (unsynced)
        /// </summary>
        public async Task<List<Booking>> GetPendingBookingsAsync()
        {
            return await _localStorage.GetItemAsync<List<Booking>>(PendingKey) ?? new List<Booking>();
        }

        /// <summary>
        /// Returns the list of synced bookings (local record of successful syncs)
        /// </summary>
        public async Task<List<Booking>> GetSyncedBookingsAsync()
        {
            return await _localStorage.GetItemAsync<List<Booking>>(SyncedKey) ?? new List<Booking>();
        }

        /// <summary>
        /// Attempts to sync pending bookings to the backend.
        /// On success, moves bookings to synced list and clears them from pending.
        /// Any failures are preserved in pending for retry.
        /// </summary>
        public async Task SyncPendingAsync()
        {
            var pending = await GetPendingBookingsAsync();
            if (!pending.Any())
                return;

            var stillPending = new List<Booking>();
            var synced = await GetSyncedBookingsAsync();

            foreach (var booking in pending)
            {
                try
                {
                    // Replace this with your real API endpoint, e.g. "https://api.rewash.co.za/bookings"
                    var response = await _http.PostAsJsonAsync("api/bookings", booking);

                    if (response.IsSuccessStatusCode)
                    {
                        booking.IsSynced = true;
                        synced.Add(booking);
                    }
                    else
                    {
                        // preserve for retry, include server message in logs if needed
                        stillPending.Add(booking);
                    }
                }
                catch (Exception ex)
                {
                    // Network error or other failure. Keep booking pending.
                    stillPending.Add(booking);
                }
            }

            // Persist updated lists
            await _localStorage.SetItemAsync(PendingKey, stillPending);
            await _localStorage.SetItemAsync(SyncedKey, synced);
        }
    }
}
