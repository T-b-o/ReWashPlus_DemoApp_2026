using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Service for managing cars in offline-first mode.
    /// </summary>
    public class CarService
    {
        private const string CarsKey = "rw_cars";
        private const string NextIdKey = "rw_car_next_id";
        
        private readonly ILocalStorageService _localStorage;
        private List<Car>? _cachedCars;
        private int _nextId = 1;

        public CarService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        /// <summary>
        /// Initialize the service
        /// </summary>
        public async Task InitializeAsync()
        {
            _cachedCars = await _localStorage.GetItemAsync<List<Car>>(CarsKey) ?? new List<Car>();
            _nextId = await _localStorage.GetItemAsync<int>(NextIdKey);
            if (_nextId == 0)
            {
                _nextId = (_cachedCars?.Count ?? 0) + 1;
            }
        }

        /// <summary>
        /// Get all cars
        /// </summary>
        public async Task<List<Car>> GetAllAsync()
        {
            await EnsureInitializedAsync();
            return _cachedCars ?? new List<Car>();
        }

        /// <summary>
        /// Get car by ID
        /// </summary>
        public async Task<Car?> GetByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            return _cachedCars?.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Get all cars for a customer
        /// </summary>
        public async Task<List<Car>> GetByCustomerIdAsync(int customerId)
        {
            await EnsureInitializedAsync();
            return _cachedCars?.Where(c => c.CustomerId == customerId && c.IsActive).ToList() ?? new List<Car>();
        }

        /// <summary>
        /// Get car by license plate
        /// </summary>
        public async Task<Car?> GetByPlateAsync(string plate)
        {
            await EnsureInitializedAsync();
            return _cachedCars?.FirstOrDefault(c => c.Plate == plate && c.IsActive);
        }

        /// <summary>
        /// Create new car
        /// </summary>
        public async Task<Car> CreateAsync(Car car)
        {
            await EnsureInitializedAsync();

            // Validate plate is unique
            if (await GetByPlateAsync(car.Plate) != null)
            {
                throw new InvalidOperationException($"Car with plate {car.Plate} already exists");
            }

            car.Id = _nextId++;
            car.CreatedAt = DateTime.UtcNow;
            car.UpdatedAt = DateTime.UtcNow;

            _cachedCars?.Add(car);
            await PersistAsync();

            return car;
        }

        /// <summary>
        /// Update car
        /// </summary>
        public async Task<Car?> UpdateAsync(Car car)
        {
            await EnsureInitializedAsync();

            var existing = _cachedCars?.FirstOrDefault(c => c.Id == car.Id);
            if (existing == null)
                return null;

            existing.CustomerId = car.CustomerId;
            existing.Plate = car.Plate;
            existing.Make = car.Make;
            existing.Model = car.Model;
            existing.Year = car.Year;
            existing.Color = car.Color;
            existing.VinNumber = car.VinNumber;
            existing.UpdatedAt = DateTime.UtcNow;

            await PersistAsync();
            return existing;
        }

        /// <summary>
        /// Delete car (soft delete)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            await EnsureInitializedAsync();

            var car = _cachedCars?.FirstOrDefault(c => c.Id == id);
            if (car == null)
                return false;

            car.IsActive = false;
            car.UpdatedAt = DateTime.UtcNow;
            await PersistAsync();

            return true;
        }

        /// <summary>
        /// Persist cars to LocalStorage
        /// </summary>
        private async Task PersistAsync()
        {
            await _localStorage.SetItemAsync(CarsKey, _cachedCars);
            await _localStorage.SetItemAsync(NextIdKey, _nextId);
        }

        /// <summary>
        /// Ensure service is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_cachedCars == null)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Clear all cars
        /// </summary>
        public async Task ClearAllAsync()
        {
            _cachedCars = new List<Car>();
            _nextId = 1;
            await PersistAsync();
        }
    }
}
