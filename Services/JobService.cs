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
    /// </summary>
    public class JobService
    {
        private const string PendingJobsKey = "rw_pending_jobs";
        private const string SyncedJobsKey = "rw_synced_jobs";
        private const string NextJobNumberKey = "rw_next_job_number";
        
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _http;
        private List<Booking>? _pendingJobs;
        private List<Booking>? _syncedJobs;
        private int _nextJobNumber = 1;

        public JobService(ILocalStorageService localStorage, HttpClient http)
        {
            _localStorage = localStorage;
            _http = http;
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
            return all.OrderByDescending(j => j.CreatedAt).ToList();
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
            return _pendingJobs ?? new List<Booking>();
        }

        /// <summary>
        /// Get synced jobs
        /// </summary>
        public async Task<List<Booking>> GetSyncedAsync()
        {
            await EnsureInitializedAsync();
            return _syncedJobs ?? new List<Booking>();
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
        /// Create a new job
        /// </summary>
        public async Task<Booking> CreateAsync(Booking job)
        {
            await EnsureInitializedAsync();

            job.Id = GetNextId();
            job.JobNumber = GenerateJobNumber();
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            job.IsSynced = false;

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

            var existing = (await GetAllAsync()).FirstOrDefault(j => j.Id == job.Id);
            if (existing == null)
                return null;

            existing.Status = job.Status;
            existing.StartedAt = job.StartedAt;
            existing.CompletedAt = job.CompletedAt;
            existing.AssignedStaffId = job.AssignedStaffId;
            existing.Notes = job.Notes;
            existing.TotalAmount = job.TotalAmount;
            existing.UpdatedAt = DateTime.UtcNow;

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
        /// Attempt to sync pending jobs to backend
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
                    // TODO: Replace with your real API endpoint
                    var response = await _http.PostAsJsonAsync("api/jobs", job);

                    if (response.IsSuccessStatusCode)
                    {
                        job.IsSynced = true;
                        _syncedJobs?.Add(job);
                    }
                    else
                    {
                        stillPending.Add(job);
                    }
                }
                catch (Exception)
                {
                    // Network error or other failure
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
