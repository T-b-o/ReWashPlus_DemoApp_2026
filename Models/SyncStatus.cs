namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Represents the offline synchronisation state of a record.
    /// Pending  — created/modified locally, not yet uploaded to the server.
    /// Synced   — successfully uploaded and acknowledged by the server.
    /// Failed   — last sync attempt resulted in a server or network error.
    /// Conflict — server returned HTTP 409 (RowVersion mismatch); requires resolution.
    /// </summary>
    public enum SyncStatus
    {
        Pending  = 0,
        Synced   = 1,
        Failed   = 2,
        Conflict = 3
    }
}
