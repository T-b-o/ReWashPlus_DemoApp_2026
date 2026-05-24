using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Service for managing customers in offline-first mode.
    /// Persists customers to LocalStorage for offline access.
    /// All operations are scoped to the current tenant.
    /// </summary>
    public class CustomerService
    {
        private const string CustomersKey = "rw_customers";
        private const string NextIdKey    = "rw_customer_next_id";

        private readonly ILocalStorageService _localStorage;
        private readonly TenantContextService _tenantContext;
        private List<Customer>? _cachedCustomers;
        private int _nextId = 1;

        public CustomerService(
            ILocalStorageService localStorage,
            TenantContextService tenantContext)
        {
            _localStorage  = localStorage;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Initialize the service - load existing customers
        /// </summary>
        public async Task InitializeAsync()
        {
            _cachedCustomers = await _localStorage.GetItemAsync<List<Customer>>(CustomersKey) ?? new List<Customer>();
            _nextId = await _localStorage.GetItemAsync<int>(NextIdKey);
            if (_nextId == 0)
            {
                _nextId = (_cachedCustomers?.Count ?? 0) + 1;
            }
        }

        /// <summary>
        /// Get all customers
        /// </summary>
        public async Task<List<Customer>> GetAllAsync()
        {
            await EnsureInitializedAsync();
            return _cachedCustomers!
                .Where(c => c.TenantId == _tenantContext.TenantId && c.IsActive)
                .ToList();
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        public async Task<Customer?> GetByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            return _cachedCustomers?.FirstOrDefault(
                c => c.Id == id &&
                     c.TenantId == _tenantContext.TenantId);
        }

        /// <summary>
        /// Search customers by phone number or name
        /// </summary>
        public async Task<List<Customer>> SearchAsync(string query)
        {
            await EnsureInitializedAsync();
            if (string.IsNullOrWhiteSpace(query))
                return _cachedCustomers ?? new List<Customer>();

            var lowerQuery = query.ToLowerInvariant();
            return _cachedCustomers?
                .Where(c =>
                    c.PhoneNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(query, StringComparison.OrdinalIgnoreCase)
                )
                .ToList() ?? new List<Customer>();
        }

        /// <summary>
        /// Get customer by phone number (unique lookup)
        /// </summary>
        public async Task<Customer?> GetByPhoneAsync(string phoneNumber)
        {
            await EnsureInitializedAsync();
            return _cachedCustomers?.FirstOrDefault(c => c.PhoneNumber == phoneNumber);
        }

        /// <summary>
        /// Create new customer
        /// </summary>
        public async Task<Customer> CreateAsync(Customer customer)
        {
            await EnsureInitializedAsync();

            // Validate phone number is unique
            if (await GetByPhoneAsync(customer.PhoneNumber) != null)
            {
                throw new InvalidOperationException($"Customer with phone {customer.PhoneNumber} already exists");
            }

            _tenantContext.ApplyContext(customer);

            customer.Id         = _nextId++;
            customer.CustomerId = Guid.NewGuid();
            customer.SyncState  = SyncStatus.Pending;
            customer.CreatedAt  = DateTime.UtcNow;
            customer.UpdatedAt  = DateTime.UtcNow;

            _cachedCustomers?.Add(customer);
            await PersistAsync();

            return customer;
        }

        /// <summary>
        /// Update existing customer
        /// </summary>
        public async Task<Customer?> UpdateAsync(Customer customer)
        {
            await EnsureInitializedAsync();

            var existing = _cachedCustomers?.FirstOrDefault(c => c.Id == customer.Id);
            if (existing == null)
                return null;

            // Update properties
            existing.FirstName = customer.FirstName;
            existing.LastName = customer.LastName;
            existing.PhoneNumber = customer.PhoneNumber;
            existing.Email = customer.Email;
            existing.PreferredPaymentMethod = customer.PreferredPaymentMethod;
            existing.TotalSpent = customer.TotalSpent;
            existing.LoyaltyPoints = customer.LoyaltyPoints;
            existing.UpdatedAt = DateTime.UtcNow;

            await PersistAsync();
            return existing;
        }

        /// <summary>
        /// Delete customer (soft delete)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            await EnsureInitializedAsync();

            var customer = _cachedCustomers?.FirstOrDefault(c => c.Id == id);
            if (customer == null)
                return false;

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            await PersistAsync();

            return true;
        }

        /// <summary>
        /// Add loyalty points to customer
        /// </summary>
        public async Task AddLoyaltyPointsAsync(int customerId, int points)
        {
            var customer = await GetByIdAsync(customerId);
            if (customer != null)
            {
                customer.LoyaltyPoints += points;
                await UpdateAsync(customer);
            }
        }

        /// <summary>
        /// Update customer total spending
        /// </summary>
        public async Task UpdateTotalSpentAsync(int customerId, decimal amount)
        {
            var customer = await GetByIdAsync(customerId);
            if (customer != null)
            {
                customer.TotalSpent += amount;
                await UpdateAsync(customer);
            }
        }

        /// <summary>
        /// Persist customers to LocalStorage
        /// </summary>
        private async Task PersistAsync()
        {
            await _localStorage.SetItemAsync(CustomersKey, _cachedCustomers);
            await _localStorage.SetItemAsync(NextIdKey, _nextId);
        }

        /// <summary>
        /// Ensure service is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_cachedCustomers == null)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Clear all customers (for testing/reset)
        /// </summary>
        public async Task ClearAllAsync()
        {
            _cachedCustomers = new List<Customer>();
            _nextId = 1;
            await PersistAsync();
        }
    }
}
