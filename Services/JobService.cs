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
    /// Service for managing jobs (bookings/wash orders) in offline-first mode.
    /// Handles creation, status updates, and syncing with backend.
    /// All new bookings are automatically scoped to the current tenant/branch.
    /// </summary>
    public class JobService
    {
        private const string PendingJobsKey   = "rw_pending_jobs";
        private const string SyncedJobsKey    = "rw_synced_jobs";
        private const string NextJobNumberKey = "rw_next_job_number";

        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient           _http;
        private readonly TenantContextService _tenantContext;
        private List<Booking>? _pendingJobs;
        private List<Booking>? _syncedJobs;
        private int _nextJobNumber = 1;

        public JobService(
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
            _pendingJobs = await _localStorage.GetItemAsync<List<Booking>>(PendingJobsKey) ?? new List<Booking>();
            _syncedJobs = await _localStorage.GetItemAsync<List<Booking>>(SyncedJobsKey) ?? new List<Booking>();
            _nextJobNumber = await _localStorage.GetItemAsync<int>(NextJobNumberKey);
            if (_nextJobNumber == 0)
            {
                _nextJobNumber = 1;
            }
        }

        /// <summary>
        /// Get all jobs (pending + synced)
        /// </summary>
        public async Task<List<Booking>> GetAllAsync()
        {
            await EnsureInitializedAsync();
            var all = new List<Booking>();
            all.AddRange(_pendingJobs ?? new List<Booking>());
            all.AddRange(_syncedJobs ?? new List<Booking>());

            return all
                .Where(j =>
                    j.TenantId == _tenantContext.TenantId &&
                    j.BranchId == _tenantContext.BranchId)
                .OrderByDescending(j => j.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get only today's jobs
        /// </summary>
        public async Task<List<Booking>> GetTodayAsync()
        {
            var all = await GetAllAsync();
            var today = DateTime.Today;
            return all.Where(j =>
                (j.ArrivedAt?.Date == today) ||
                (j.CreatedAt.Date == today && j.Type == JobType.WalkIn)
            ).ToList();
        }

        /// <summary>
        /// Get active jobs (not completed or cancelled)
        /// </summary>
        public async Task<List<Booking>> GetActiveAsync()
        {
            var all = await GetAllAsync();
            return all.Where(j => j.IsActive).OrderBy(j => j.CreatedAt).ToList();
        }

        /// <summary>
        /// Get pending jobs (not yet synced to backend)
        /// </summary>
        public async Task<List<Booking>> GetPendingAsync()
        {
            await EnsureInitializedAsync();
            return (_pendingJobs ?? new List<Booking>())
                .Where(j =>
                    j.TenantId == _tenantContext.TenantId &&
                    j.BranchId == _tenantContext.BranchId)
                .ToList();
        }

        /// <summary>
        /// Get synced jobs
        /// </summary>
        public async Task<List<Booking>> GetSyncedAsync()
        {
            await EnsureInitializedAsync();
            return (_syncedJobs ?? new List<Booking>())
                .Where(j =>
                    j.TenantId == _tenantContext.TenantId &&
                    j.BranchId == _tenantContext.BranchId)
                .ToList();
        }

        /// <summary>
        /// Get job by ID
        /// </summary>
        public async Task<Booking?> GetByIdAsync(int id)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(j => j.Id == id);
        }

        /// <summary>
        /// Get jobs for a customer
        /// </summary>
        public async Task<List<Booking>> GetByCustomerIdAsync(int customerId)
        {
            var all = await GetAllAsync();
            return all.Where(j => j.CustomerId == customerId).ToList();
        }

        /// <summary>
        /// Get jobs for a car
        /// </summary>
        public async Task<List<Booking>> GetByCarIdAsync(int carId)
        {
            var all = await GetAllAsync();
            return all.Where(j => j.CarId == carId).ToList();
        }

        /// <summary>
        /// Create a new job. Automatically stamps TenantId and BranchId from context.
        /// </summary>
        public async Task<Booking> CreateAsync(Booking job)
        {
            await EnsureInitializedAsync();

            _tenantContext.ApplyContext(job);

            job.Id        = GetNextId();
            job.BookingId = Guid.NewGuid();
            job.JobNumber = GenerateJobNumber();
            job.SyncState = SyncStatus.Pending;
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            if (job.Type == JobType.WalkIn)
            {
                job.ArrivedAt = DateTime.UtcNow;
            }

            _pendingJobs?.Add(job);
            await PersistAsync();

            return job;
        }

        /// <summary>
        /// Update job status and details
        /// </summary>
        public async Task<Booking?> UpdateAsync(Booking job)
        {
            await EnsureInitializedAsync();

            var existing = FindStoredJob(job.Id);
            if (existing == null)
                return null;

            existing.Status                = job.Status;
            existing.StartedAt             = job.StartedAt;
            existing.CompletedAt           = job.CompletedAt;
            existing.AssignedStaffId       = job.AssignedStaffId;
            existing.AssignedStaffProfileId = job.AssignedStaffProfileId;
            existing.Notes                 = job.Notes;
            existing.TotalAmount           = job.TotalAmount;
            existing.SyncState             = SyncStatus.Pending;
            existing.UpdatedAt             = DateTime.UtcNow;

            MoveToPendingIfNeeded(existing);
            await PersistAsync();
            return existing;
        }

        /// <summary>
        /// Mark job as started
        /// </summary>
        public async Task<Booking?> StartJobAsync(int jobId)
        {
            var job = await GetByIdAsync(jobId);
            if (job == null)
                return null;

            job.Status = JobStatus.InProgress;
            job.StartedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(job);
        }

        /// <summary>
        /// Mark job as completed
        /// </summary>
        public async Task<Booking?> CompleteJobAsync(int jobId)
        {
            var job = await GetByIdAsync(jobId);
            if (job == null)
                return null;

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(job);
        }

        /// <summary>
        /// Cancel a job
        /// </summary>
        public async Task<Booking?> CancelJobAsync(int jobId, string reason = "")
        {
            var job = await GetByIdAsync(jobId);
            if (job == null)
                return null;

            job.Status = JobStatus.Cancelled;
            job.Notes = $"Cancelled: {reason}".TrimEnd(':');
            job.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(job);
        }

        /// <summary>
        /// Add service to a job
        /// </summary>
        public async Task<Booking?> AddServiceAsync(int jobId, Service service, int quantity = 1)
        {
            var job = await GetByIdAsync(jobId);
            if (job == null)
                return null;

            var jobService = new Models.JobService
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                Quantity = quantity,
                PriceAtTime = service.Price,
                AddedAt = DateTime.UtcNow
            };

            job.Services.Add(jobService);
            job.RecalculateTotal();
            job.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(job);
        }

        /// <summary>
        /// Remove service from a job
        /// </summary>
        public async Task<Booking?> RemoveServiceAsync(int jobId, int serviceId)
        {
            var job = await GetByIdAsync(jobId);
            if (job == null)
                return null;

            job.Services.RemoveAll(s => s.ServiceId == serviceId);
            job.RecalculateTotal();
            job.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(job);
        }

        /// <summary>
        /// Attempt to sync pending jobs to the backend API.
        /// Handles HTTP 409 Conflict (RowVersion mismatch) separately from other failures.
        /// </summary>
        public async Task SyncPendingAsync()
        {
            await EnsureInitializedAsync();

            if (!(_pendingJobs?.Any() ?? false))
                return;

            var stillPending = new List<Booking>();

            foreach (var job in _pendingJobs)
            {
                try
                {
                    var response = await _http.PostAsJsonAsync("api/v1/bookings", job);

                    if (response.IsSuccessStatusCode)
                    {
                        job.SyncState = SyncStatus.Synced;
                        _syncedJobs?.Add(job);
                    }
                    else if ((int)response.StatusCode == 409)
                    {
                        // Conflict: server has a newer version — flag for manual resolution
                        job.SyncState = SyncStatus.Conflict;
                        stillPending.Add(job);
                    }
                    else
                    {
                        job.SyncState = SyncStatus.Failed;
                        stillPending.Add(job);
                    }
                }
                catch (Exception)
                {
                    // Network error — keep pending for next retry
                    stillPending.Add(job);
                }
            }

            _pendingJobs = stillPending;
            await PersistAsync();
        }

        /// <summary>
        /// Get summary statistics for dashboard
        /// </summary>
        public async Task<(int Total, int Active, int Completed, decimal Revenue)> GetDailySummaryAsync()
        {
            var today = await GetTodayAsync();
            var completed = today.Where(j => j.Status == JobStatus.Completed).ToList();
            var revenue = completed.Sum(j => j.TotalAmount);

            return (today.Count, today.Count(j => j.IsActive), completed.Count, revenue);
        }

        /// <summary>
        /// Generate next job number (e.g., JOB-20250120-001)
        /// </summary>
        private string GenerateJobNumber()
        {
            var today = DateTime.Today.ToString("yyyyMMdd");
            var sequence = _nextJobNumber.ToString("D3");
            _nextJobNumber++;
            return $"JOB-{today}-{sequence}";
        }

        /// <summary>
        /// Get next unique ID
        /// </summary>
        private int GetNextId()
        {
            var all = new List<Booking>();
            all.AddRange(_pendingJobs ?? new List<Booking>());
            all.AddRange(_syncedJobs ?? new List<Booking>());
            return (all.Max(j => (int?)j.Id) ?? 0) + 1;
        }

        /// <summary>
        /// Finds a booking in the backing collections without creating a detached copy.
        /// The tenant and branch checks prevent cross-tenant edits from browser storage.
        /// </summary>
        private Booking? FindStoredJob(int id)
        {
            return (_pendingJobs ?? new List<Booking>())
                .Concat(_syncedJobs ?? new List<Booking>())
                .FirstOrDefault(j =>
                    j.Id == id &&
                    j.TenantId == _tenantContext.TenantId &&
                    j.BranchId == _tenantContext.BranchId);
        }

        /// <summary>
        /// A synced record edited offline must re-enter the pending queue so the
        /// next sync cycle uploads the changed version.
        /// </summary>
        private void MoveToPendingIfNeeded(Booking job)
        {
            if (_pendingJobs?.Any(j => j.Id == job.Id) == true)
                return;

            if (_syncedJobs?.Remove(job) == true)
                _pendingJobs?.Add(job);
        }

        /// <summary>
        /// Persist jobs to LocalStorage
        /// </summary>
        private async Task PersistAsync()
        {
            await _localStorage.SetItemAsync(PendingJobsKey, _pendingJobs);
            await _localStorage.SetItemAsync(SyncedJobsKey, _syncedJobs);
            await _localStorage.SetItemAsync(NextJobNumberKey, _nextJobNumber);
        }

        /// <summary>
        /// Ensure service is initialized
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_pendingJobs == null)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Clear all jobs (for testing)
        /// </summary>
        public async Task ClearAllAsync()
        {
            _pendingJobs = new List<Booking>();
            _syncedJobs = new List<Booking>();
            _nextJobNumber = 1;
            await PersistAsync();
        }
    }
}
