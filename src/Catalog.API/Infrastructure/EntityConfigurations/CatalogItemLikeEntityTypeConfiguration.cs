namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class CatalogItemLikeEntityTypeConfiguration : IEntityTypeConfiguration<CatalogItemLike>
{
    public void Configure(EntityTypeBuilder<CatalogItemLike> builder)
    {
        builder.ToTable("CatalogItemLikes");

        builder.Property(l => l.UserId)
            .HasMaxLength(256);

        builder.HasOne(l => l.CatalogItem)
            .WithMany()
            .HasForeignKey(l => l.CatalogItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.CatalogItemId, l.UserId })
            .IsUnique();
    }
}
