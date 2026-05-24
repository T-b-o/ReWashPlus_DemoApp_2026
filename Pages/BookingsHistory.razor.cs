using Microsoft.AspNetCore.Components;
using ReWashPlus_DemoApp.Models;
using ReWashPlus_DemoApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReWashPlus_DemoApp.Pages
{
    /// <summary>
    /// Code-behind for BookingsHistory.razor.
    /// Uses the canonical Models.Booking type with AppointmentAt and JobStatus.
    /// </summary>
    public partial class BookingsHistory : ComponentBase
    {
        #region State

        protected string SearchTerm { get; set; } = string.Empty;

        private List<Booking> _allBookings = new();

        protected IEnumerable<Booking> FilteredBookings =>
            string.IsNullOrWhiteSpace(SearchTerm)
                ? _allBookings
                : _allBookings.Where(b =>
                    (!string.IsNullOrEmpty(b.CustomerName) &&
                     b.CustomerName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    b.Id.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

        #endregion

        #region Services

        [Inject] protected NavigationManager                       Nav              { get; set; } = default!;
        [Inject] protected PageTitleService                        PageTitleService { get; set; } = default!;
        [Inject] protected ReWashPlus_DemoApp.Services.JobService  JobService       { get; set; } = default!;

        #endregion

        #region Lifecycle

        protected override void OnInitialized()
        {
            PageTitleService.TitleChanged += () => InvokeAsync(StateHasChanged);

            // Seed with representative data that uses the real Booking model.
            // Replace with: _allBookings = await JobService.GetAllAsync();
            _allBookings = new List<Booking>
            {
                new Booking
                {
                    Id            = 1001,
                    CustomerName  = "John Doe",
                    PhoneNumber   = "0812345678",
                    AppointmentAt = DateTime.Today.AddHours(10),
                    Status        = JobStatus.Waiting,
                    Type          = JobType.PreBooked
                },
                new Booking
                {
                    Id            = 1002,
                    CustomerName  = "Jane Smith",
                    PhoneNumber   = "0823456789",
                    AppointmentAt = DateTime.Today.AddHours(11).AddMinutes(30),
                    Status        = JobStatus.InProgress,
                    Type          = JobType.WalkIn
                },
                new Booking
                {
                    Id            = 1003,
                    CustomerName  = "Mark Lee",
                    PhoneNumber   = "0834567890",
                    AppointmentAt = DateTime.Today.AddDays(1).AddHours(13),
                    Status        = JobStatus.Cancelled,
                    Type          = JobType.PreBooked
                }
            };
        }

        #endregion

        #region UI Helpers

        /// <summary>Returns Tailwind badge CSS class for the given JobStatus.</summary>
        protected string GetStatusBadgeClass(JobStatus status) =>
            status switch
            {
                JobStatus.Waiting    => "status-waiting",
                JobStatus.InProgress => "status-inprogress",
                JobStatus.Completed  => "status-completed",
                JobStatus.Cancelled  => "status-cancelled",
                _                    => "status-waiting"
            };

        #endregion

        #region Actions

        protected void ViewBooking(int id)
            => Nav.NavigateTo($"/bookings/{id}", forceLoad: false);

        protected void EditBooking(int id)
            => Nav.NavigateTo($"/bookings/{id}/edit", forceLoad: false);

        protected async void CancelBooking(int id)
        {
            await JobService.CancelJobAsync(id, "Cancelled by user");
            var booking = _allBookings.FirstOrDefault(b => b.Id == id);
            if (booking is not null)
            {
                booking.Status = JobStatus.Cancelled;
                StateHasChanged();
            }
        }

        #endregion
    }
}
