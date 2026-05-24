using System;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Central service for managing the current page title.
    /// Pages call SetTitle() when loaded.
    /// The layout subscribes to TitleChanged and updates the UI.
    /// </summary>
    public class PageTitleService
    {
        private string _title = "Home";

        /// <summary>
        /// Gets the current page title.
        /// </summary>
        public string Title => _title;

        /// <summary>
        /// Raised when the title changes.
        /// </summary>
        public event Action? TitleChanged;

        /// <summary>
        /// Updates the current page title and notifies listeners.
        /// </summary>
        /// <param name="title">The display-friendly page title.</param>
        public void SetTitle(string title)
        {
            if (!string.Equals(_title, title, StringComparison.Ordinal))
            {
                _title = title;
                TitleChanged?.Invoke();
            }
        }
    }
}
