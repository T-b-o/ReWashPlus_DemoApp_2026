using Microsoft.JSInterop;

namespace ReWashPlus_DemoApp.Services
{
    /// <summary>
    /// Tracks browser online/offline state via JS interop.
    /// Subscribe to <see cref="ConnectivityChanged"/> to react when connectivity flips.
    /// Call <see cref="InitializeAsync"/> once in App.razor OnInitializedAsync.
    /// </summary>
    public class ConnectivityService : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private DotNetObjectReference<ConnectivityService>? _selfRef;
        private bool _initialized;

        /// <summary>True when the browser reports navigator.onLine.</summary>
        public bool IsOnline { get; private set; } = true;

        /// <summary>Fired on every online ↔ offline transition.</summary>
        public event Action? ConnectivityChanged;

        public ConnectivityService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// Read current online state and register browser event listeners.
        /// Safe to call multiple times — no-op after first call.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            IsOnline = await _js.InvokeAsync<bool>("navigator.onLine");
            _selfRef = DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("rewashConnectivity.initialize", _selfRef);
            _initialized = true;
        }

        /// <summary>
        /// Invoked by JavaScript when the browser online/offline event fires.
        /// </summary>
        [JSInvokable]
        public void OnConnectivityChanged(bool isOnline)
        {
            IsOnline = isOnline;
            ConnectivityChanged?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            if (_selfRef is not null)
            {
                try
                {
                    await _js.InvokeVoidAsync("rewashConnectivity.dispose");
                }
                catch
                {
                    // Swallow — JS context may already be torn down during hot-reload
                }
                finally
                {
                    _selfRef.Dispose();
                    _selfRef = null;
                }
            }
        }
    }
}
