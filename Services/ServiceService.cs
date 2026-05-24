using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Service for managing services (service catalog with pricing).
    /// Services are typically cached and don't change often.
    /// </summary>
    public class ServiceService
    {
        private const string ServicesKey = "rw_services";
        private const string LastSyncKey = "rw_services_last_sync";
        private const int CacheDurationHours = 24;
        
        private readonly ILocalStorageService _localStorage;
        private List<Service>? _cachedServices;

        public ServiceService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        /// <summary>
        /// Initialize and load services
        /// </summary>
        public async Task InitializeAsync()
        {
            _cachedServices = await _localStorage.GetItemAsync<List<Service>>(ServicesKey);
            
            // If no cached services, seed with defaults
            if (_cachedServices == null || !_cachedServices.Any())
            {
                _cachedServices = GetDefaultServices();
                await PersistAsync();
            }
        }

        /// <summary>
        /// Get all active services
        /// </summary>
        public async Task<List<Service>> GetAllAsync()
        {
            await EnsureInitializedAsync();
            return _cachedServices?.Where(s => s.IsActive).ToList() ?? new List<Service>();
        }

        /// <summary>
        /// Get service by ID
        /// </summary>
        public async Task<Service?> GetByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            return _cachedServices?.FirstOrDefault(s => s.Id == id && s.IsActive);
        }

        /// <summary>
        /// Get services by name (partial match)
        /// </summary>
        public async Task<List<Service>> SearchAsync(string query)
        {
            await EnsureInitializedAsync();
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            return _cachedServices?
                .Where(s => s.IsActive && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<Service>();
        }

        /// <summary>
        /// Calculate total price for multiple services
        /// </summary>
        public async Task<decimal> CalculateTotalAsync(List<int> serviceIds)
        {
            var services = await GetAllAsync();
            return services
                .Where(s => serviceIds.Contains(s.Id))
                .Sum(s => s.Price);
        }

        /// <summary>
        /// Ensure service is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_cachedServices == null)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Persist services to LocalStorage
        /// </summary>
        private async Task PersistAsync()
        {
            await _localStorage.SetItemAsync(ServicesKey, _cachedServices);
            await _localStorage.SetItemAsync(LastSyncKey, DateTime.UtcNow);
        }

        /// <summary>
        /// Get default services for initial setup
        /// </summary>
        private static List<Service> GetDefaultServices()
        {
            return new List<Service>
            {
                new Service
                {
                    Id = 1,
                    Name = "Basic Wash",
                    Description = "Exterior wash with water and soap",
                    Price = 150,
                    DurationMinutes = 20,
                    IsActive = true
                },
                new Service
                {
                    Id = 2,
                    Name = "Deluxe Wash",
                    Description = "Exterior wash with wax and polish",
                    Price = 250,
                    DurationMinutes = 35,
                    IsActive = true
                },
                new Service
                {
                    Id = 3,
                    Name = "Interior Clean",
                    Description = "Vacuum and wipe down interior",
                    Price = 200,
                    DurationMinutes = 30,
                    IsActive = true
                },
                new Service
                {
                    Id = 4,
                    Name = "Full Valet",
                    Description = "Complete exterior and interior service",
                    Price = 500,
                    DurationMinutes = 90,
                    IsActive = true
                },
                new Service
                {
                    Id = 5,
                    Name = "Engine Wash",
                    Description = "Engine bay cleaning and degreasing",
                    Price = 200,
                    DurationMinutes = 45,
                    IsActive = true
                },
                new Service
                {
                    Id = 6,
                    Name = "Tire Shine",
                    Description = "Tire cleaning and shine application",
                    Price = 100,
                    DurationMinutes = 15,
                    IsActive = true
                }
            };
        }
    }
}
