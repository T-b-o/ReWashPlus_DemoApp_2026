using Blazored.LocalStorage;
using ReWashPlus_DemoApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Service for managing payments in offline-first mode.
    /// All operations are scoped to the current tenant and branch.
    /// </summary>
    public class PaymentService
    {
        private const string PendingPaymentsKey = "rw_pending_payments";
        private const string SyncedPaymentsKey  = "rw_synced_payments";

        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient           _http;
        private readonly TenantContextService _tenantContext;
        private List<Payment>? _pendingPayments;
        private List<Payment>? _syncedPayments;

        public PaymentService(
            ILocalStorageService localStorage,
            HttpClient           http,
            TenantContextService tenantContext)
        {
            _localStorage  = localStorage;
            _http          = http;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Initialize the service
        /// </summary>
        public async Task InitializeAsync()
        {
            _pendingPayments = await _localStorage.GetItemAsync<List<Payment>>(PendingPaymentsKey) ?? new List<Payment>();
            _syncedPayments = await _localStorage.GetItemAsync<List<Payment>>(SyncedPaymentsKey) ?? new List<Payment>();
        }

        /// <summary>
        /// Get all payments (pending + synced)
        /// </summary>
        public async Task<List<Payment>> GetAllAsync()
        {
            await EnsureInitializedAsync();
            var all = new List<Payment>();
            all.AddRange(_pendingPayments ?? new List<Payment>());
            all.AddRange(_syncedPayments ?? new List<Payment>());
            return all
                .Where(p =>
                    p.TenantId == _tenantContext.TenantId &&
                    p.BranchId == _tenantContext.BranchId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get payment by ID
        /// </summary>
        public async Task<Payment?> GetByIdAsync(int id)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Get payments for a job
        /// </summary>
        public async Task<List<Payment>> GetByJobIdAsync(int jobId)
        {
            var all = await GetAllAsync();
            return all.Where(p => p.JobId == jobId).ToList();
        }

        /// <summary>
        /// Get pending (unsynced) payments
        /// </summary>
        public async Task<List<Payment>> GetPendingAsync()
        {
            await EnsureInitializedAsync();
            return (_pendingPayments ?? new List<Payment>())
                .Where(p =>
                    p.TenantId == _tenantContext.TenantId &&
                    p.BranchId == _tenantContext.BranchId)
                .ToList();
        }

        /// <summary>
        /// Get synced payments
        /// </summary>
        public async Task<List<Payment>> GetSyncedAsync()
        {
            await EnsureInitializedAsync();
            return (_syncedPayments ?? new List<Payment>())
                .Where(p =>
                    p.TenantId == _tenantContext.TenantId &&
                    p.BranchId == _tenantContext.BranchId)
                .ToList();
        }

        /// <summary>
        /// Record a new payment
        /// </summary>
        public async Task<Payment> RecordPaymentAsync(int jobId, decimal amount, PaymentMethod method, string? reference = null)
        {
            await EnsureInitializedAsync();

            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");

            var payment = new Payment
            {
                Id          = GetNextId(),
                PaymentId   = Guid.NewGuid(),
                JobId       = jobId,
                TenantId    = _tenantContext.TenantId,
                BranchId    = _tenantContext.BranchId,
                Amount      = amount,
                Method      = method,
                Status      = PaymentStatus.Completed,
                SyncState   = SyncStatus.Pending,
                Reference = reference ?? "",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _pendingPayments?.Add(payment);
            await PersistAsync();

            return payment;
        }

        /// <summary>
        /// Update payment status
        /// </summary>
        public async Task<Payment?> UpdateStatusAsync(int paymentId, PaymentStatus status)
        {
            var payment = await GetByIdAsync(paymentId);
            if (payment == null)
                return null;

            payment.Status = status;
            if (status == PaymentStatus.Completed && !payment.PaidAt.HasValue)
            {
                payment.PaidAt = DateTime.UtcNow;
            }
            payment.UpdatedAt = DateTime.UtcNow;
            payment.SyncState = SyncStatus.Pending;

            MoveToPendingIfNeeded(payment);
            await PersistAsync();
            return payment;
        }

        /// <summary>
        /// Get daily revenue summary
        /// </summary>
        public async Task<(int TotalPayments, decimal TotalRevenue)> GetDailySummaryAsync()
        {
            var today = DateTime.Today;
            var all = await GetAllAsync();
            var todayPayments = all.Where(p =>
                p.Status == PaymentStatus.Completed &&
                p.PaidAt?.Date == today
            ).ToList();

            return (todayPayments.Count, todayPayments.Sum(p => p.Amount));
        }

        /// <summary>
        /// Get revenue by payment method
        /// </summary>
        public async Task<Dictionary<PaymentMethod, decimal>> GetRevenueByMethodAsync(DateTime startDate, DateTime endDate)
        {
            var all = await GetAllAsync();
            var filtered = all.Where(p =>
                p.Status == PaymentStatus.Completed &&
                p.PaidAt.HasValue &&
                p.PaidAt.Value.Date >= startDate.Date &&
                p.PaidAt.Value.Date <= endDate.Date
            );

            return filtered
                .GroupBy(p => p.Method)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
        }

        /// <summary>
        /// Attempt to sync pending payments to backend
        /// </summary>
        public async Task SyncPendingAsync()
        {
            await EnsureInitializedAsync();

            if (!(_pendingPayments?.Any() ?? false))
                return;

            var stillPending = new List<Payment>();

            foreach (var payment in _pendingPayments)
            {
                try
                {
                    var response = await _http.PostAsJsonAsync("api/v1/payments", payment);

                    if (response.IsSuccessStatusCode)
                    {
                        payment.SyncState = SyncStatus.Synced;
                        _syncedPayments?.Add(payment);
                    }
                    else if ((int)response.StatusCode == 409)
                    {
                        payment.SyncState = SyncStatus.Conflict;
                        stillPending.Add(payment);
                    }
                    else
                    {
                        payment.SyncState = SyncStatus.Failed;
                        stillPending.Add(payment);
                    }
                }
                catch (Exception)
                {
                    // Network error or other failure
                    stillPending.Add(payment);
                }
            }

            _pendingPayments = stillPending;
            await PersistAsync();
        }

        /// <summary>
        /// Get next unique ID
        /// </summary>
        private int GetNextId()
        {
            var all = new List<Payment>();
            all.AddRange(_pendingPayments ?? new List<Payment>());
            all.AddRange(_syncedPayments ?? new List<Payment>());
            return (all.Max(p => (int?)p.Id) ?? 0) + 1;
        }

        /// <summary>
        /// Keeps edited synced payments eligible for the next offline sync.
        /// </summary>
        private void MoveToPendingIfNeeded(Payment payment)
        {
            if (_pendingPayments?.Any(p => p.Id == payment.Id) == true)
                return;

            if (_syncedPayments?.Remove(payment) == true)
                _pendingPayments?.Add(payment);
        }

        /// <summary>
        /// Persist payments to LocalStorage
        /// </summary>
        private async Task PersistAsync()
        {
            await _localStorage.SetItemAsync(PendingPaymentsKey, _pendingPayments);
            await _localStorage.SetItemAsync(SyncedPaymentsKey, _syncedPayments);
        }

        /// <summary>
        /// Ensure service is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_pendingPayments == null)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Clear all payments (for testing)
        /// </summary>
        public async Task ClearAllAsync()
        {
            _pendingPayments = new List<Payment>();
            _syncedPayments = new List<Payment>();
            await PersistAsync();
        }
    }
}
