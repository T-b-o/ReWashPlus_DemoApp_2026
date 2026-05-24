namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Marks an entity with standard audit timestamps.
    /// </summary>
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }
}
