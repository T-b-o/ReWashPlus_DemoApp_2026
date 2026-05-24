/**
 * rewashConnectivity — Browser online/offline bridge for Blazor ConnectivityService.
 * Registers window online/offline event listeners and notifies the .NET side
 * via DotNetObjectReference.invokeMethodAsync.
 *
 * Usage (C#):
 *   await JS.InvokeVoidAsync("rewashConnectivity.initialize", dotNetRef);
 *   await JS.InvokeVoidAsync("rewashConnectivity.dispose");
 */
window.rewashConnectivity = (function () {
    let _dotNetRef = null;
    let _onlineHandler = null;
    let _offlineHandler = null;

    return {
        initialize: function (dotNetRef) {
            _dotNetRef = dotNetRef;

            _onlineHandler = function () {
                if (_dotNetRef) {
                    _dotNetRef.invokeMethodAsync('OnConnectivityChanged', true);
                }
            };

            _offlineHandler = function () {
                if (_dotNetRef) {
                    _dotNetRef.invokeMethodAsync('OnConnectivityChanged', false);
                }
            };

            window.addEventListener('online',  _onlineHandler);
            window.addEventListener('offline', _offlineHandler);
        },

        dispose: function () {
            if (_onlineHandler)  window.removeEventListener('online',  _onlineHandler);
            if (_offlineHandler) window.removeEventListener('offline', _offlineHandler);
            _dotNetRef      = null;
            _onlineHandler  = null;
            _offlineHandler = null;
        },

        isOnline: function () {
            return navigator.onLine;
        }
    };
}());
