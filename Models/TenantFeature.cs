namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Controls which platform features a tenant has enabled.
    /// UI components and API endpoints check IsEnabled before rendering or executing.
    /// Use <see cref="FeatureCodes"/> constants for FeatureCode values.
    /// </summary>
    public class TenantFeature
    {
        public int    Id          { get; set; }
        public Guid   TenantId   { get; set; }

        /// <summary>Use <see cref="FeatureCodes"/> constants — never a raw string literal.</summary>
        public string FeatureCode { get; set; } = string.Empty;

        public bool   IsEnabled  { get; set; }
    }
}
