using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiModelEvalCenter.Infrastructure.Persistence.Configurations
{
    public class DriftMetricConfiguration : IEntityTypeConfiguration<DriftMetric>
    {
        public void Configure(EntityTypeBuilder<DriftMetric> builder)
        {
            builder.HasKey(d => d.Id);
            
            builder.HasOne(d => d.Inference)
                   .WithOne()
                   .HasForeignKey<DriftMetric>(d => d.InferenceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.GroundTruth)
                   .WithMany()
                   .HasForeignKey(d => d.GroundTruthId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(d => d.IouScore).HasColumnType("numeric(5,4)");
            builder.Property(d => d.ConfidenceDelta).HasColumnType("numeric(5,4)");

            builder.HasIndex(d => d.GroundTruthId);
        }
    }
}
