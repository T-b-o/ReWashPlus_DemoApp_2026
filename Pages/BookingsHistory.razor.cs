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

        protected override async Task OnInitializedAsync()
        {
            PageTitleService.TitleChanged += () => InvokeAsync(StateHasChanged);
            _allBookings = await JobService.GetAllAsync();
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
