using Blazored.LocalStorage;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Represents a car wash location visible to the customer.
    /// </summary>
    public class CarWashLocation
    {
        public string Id         { get; init; } = string.Empty;
        public string Name       { get; init; } = string.Empty;
        public string Address    { get; init; } = string.Empty;
        public double DistanceKm { get; init; }
        public bool   IsOpen     { get; init; }
        public string OpenHours  { get; init; } = string.Empty;
    }

    /// <summary>
    /// Provides a static list of demo nearby car wash locations (sorted by distance)
    /// and persists the customer's last-used selection in browser LocalStorage.
    /// </summary>
    public class CarWashLocationService
    {
        private const string LastWashKey = "rw_last_wash_id";
        private readonly ILocalStorageService _localStorage;

        /// <summary>Demo car wash locations, sorted nearest-first.</summary>
        public static readonly IReadOnlyList<CarWashLocation> AllLocations = new List<CarWashLocation>
        {
            new() { Id = "1", Name = "Fourways Mobile Team",  Address = "Fourways Mall, Sandton",         DistanceKm = 0.4, IsOpen = true,  OpenHours = "07:00 – 18:00" },
            new() { Id = "2", Name = "Sandton City Wash",     Address = "Sandton City, Sandton",           DistanceKm = 2.1, IsOpen = true,  OpenHours = "08:00 – 17:00" },
            new() { Id = "3", Name = "Rivonia Road Wash",     Address = "Rivonia Rd, Sandton",             DistanceKm = 3.5, IsOpen = false, OpenHours = "08:00 – 16:00" },
            new() { Id = "4", Name = "Hyde Park Corner Wash", Address = "Hyde Park Corner, Johannesburg",  DistanceKm = 5.2, IsOpen = true,  OpenHours = "07:30 – 17:30" },
            new() { Id = "5", Name = "Rosebank Wash Centre",  Address = "Rosebank Mall, Johannesburg",     DistanceKm = 7.8, IsOpen = true,  OpenHours = "06:00 – 19:00" },
        };

        public CarWashLocationService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        /// <summary>Returns the ID of the last car wash the customer booked at, or null.</summary>
        public ValueTask<string?> GetLastUsedIdAsync()
            => _localStorage.GetItemAsync<string?>(LastWashKey);

        /// <summary>Persists the last-used car wash ID so it can be recalled next visit.</summary>
        public ValueTask SetLastUsedAsync(string id)
            => _localStorage.SetItemAsync(LastWashKey, id);

        /// <summary>Looks up a location by ID from the static list.</summary>
        public static CarWashLocation? GetById(string? id)
            => AllLocations.FirstOrDefault(l => l.Id == id);
    }
}
