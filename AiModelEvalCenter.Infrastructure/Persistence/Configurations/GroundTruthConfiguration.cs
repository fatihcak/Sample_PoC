using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiModelEvalCenter.Infrastructure.Persistence.Configurations
{
    public class GroundTruthConfiguration : IEntityTypeConfiguration<GroundTruth>
    {
        public void Configure(EntityTypeBuilder<GroundTruth> builder)
        {
            builder.HasKey(g => g.Id);
            
            builder.HasOne(g => g.Frame)
                   .WithOne()
                   .HasForeignKey<GroundTruth>(g => g.FrameId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(g => g.VerifiedBy).HasMaxLength(150).IsRequired();
            builder.Property(g => g.VerificationMethod).HasMaxLength(100).IsRequired();
            builder.Property(g => g.TrueClass).HasMaxLength(150).IsRequired();
            
            builder.OwnsOne(g => g.BoundingBox, bb =>
            {
                bb.ToJson();
            });
        }
    }
}
