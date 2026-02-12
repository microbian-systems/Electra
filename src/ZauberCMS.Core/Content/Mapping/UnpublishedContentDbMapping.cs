using ZauberCMS.Core.Content.Models;

namespace ZauberCMS.Core.Content.Mapping;

public class UnpublishedContentDbMapping : IEntityTypeConfiguration<UnpublishedContent>
{
    public void Configure(EntityTypeBuilder<UnpublishedContent> builder)
    {
        builder.ToTable("ZauberUnpublishedContent");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.DateCreated).IsRequired();
        builder.Property(x => x.DateUpdated).IsRequired();
        builder.Property(e => e.JsonContent).ToJsonConversion(null);
        
        
    }
}