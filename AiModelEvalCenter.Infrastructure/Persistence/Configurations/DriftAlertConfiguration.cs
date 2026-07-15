using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiModelEvalCenter.Infrastructure.Persistence.Configurations
{
    public class DriftAlertConfiguration : IEntityTypeConfiguration<DriftAlert>
    {
        public void Configure(EntityTypeBuilder<DriftAlert> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TriggeredAtIou).HasPrecision(5, 4);
            builder.Property(x => x.Threshold).HasPrecision(5, 4);
            builder.Property(x => x.Severity).HasMaxLength(20);

            builder.HasOne(x => x.Model)
                .WithMany()
                .HasForeignKey(x => x.ModelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ModelId, x.DetectedAt });
        }
    }
}
