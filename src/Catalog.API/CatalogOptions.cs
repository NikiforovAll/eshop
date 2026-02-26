namespace eShop.Catalog.API;

public class CatalogOptions
{
    public string? PicBaseUrl { get; set; }
    public bool UseCustomizationData { get; set; }
    public int LikeCacheDurationSeconds { get; set; } = 30;
}
