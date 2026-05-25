/**
 * rewashConnectivity — Browser online/offline bridge for Blazor ConnectivityService.
 * Registers window online/offline event listeners and notifies the .NET side
 * via DotNetObjectReference.invokeMethodAsync.
 *
 * Also polls navigator.onLine every 3 seconds as a fallback, because browser
 * online/offline events are not always fired promptly on desktop (e.g. when
 * Wi-Fi is turned off but a LAN adapter is still present).
 *
 * Usage (C#):
 *   await JS.InvokeVoidAsync("rewashConnectivity.initialize", dotNetRef);
 *   await JS.InvokeVoidAsync("rewashConnectivity.dispose");
 */
window.rewashConnectivity = (function () {
    let _dotNetRef      = null;
    let _onlineHandler  = null;
    let _offlineHandler = null;
    let _pollTimer      = null;
    let _lastKnown      = navigator.onLine;

    function notify(isOnline) {
        if (_dotNetRef) {
            _dotNetRef.invokeMethodAsync('OnConnectivityChanged', isOnline)
                      .catch(() => { /* component may have been disposed */ });
        }
    }

    function poll() {
        const current = navigator.onLine;
        if (current !== _lastKnown) {
            _lastKnown = current;
            notify(current);
        }
    }

    return {
        initialize: function (dotNetRef) {
            _dotNetRef  = dotNetRef;
            _lastKnown  = navigator.onLine;

            _onlineHandler = function () {
                _lastKnown = true;
                notify(true);
            };

            _offlineHandler = function () {
                _lastKnown = false;
                notify(false);
            };

            window.addEventListener('online',  _onlineHandler);
            window.addEventListener('offline', _offlineHandler);

            // Fallback poll — catches cases where browser events fire late
            _pollTimer = setInterval(poll, 3000);
        },

        dispose: function () {
            if (_onlineHandler)  window.removeEventListener('online',  _onlineHandler);
            if (_offlineHandler) window.removeEventListener('offline', _offlineHandler);
            if (_pollTimer)      clearInterval(_pollTimer);
            _dotNetRef      = null;
            _onlineHandler  = null;
            _offlineHandler = null;
            _pollTimer      = null;
        },

        isOnline: function () {
            return navigator.onLine;
        }
    };
}());
