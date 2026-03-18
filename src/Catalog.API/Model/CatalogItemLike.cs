namespace eShop.Catalog.API.Model;

public record CatalogItemLikeResult(int Count, bool IsLiked);

public class CatalogItemLike
{
    public int Id { get; set; }
    public int CatalogItemId { get; set; }
    public CatalogItem CatalogItem { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
