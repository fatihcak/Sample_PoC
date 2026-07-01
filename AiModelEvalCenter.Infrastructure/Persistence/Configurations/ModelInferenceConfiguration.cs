using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiModelEvalCenter.Infrastructure.Persistence.Configurations
{
    public class ModelInferenceConfiguration : IEntityTypeConfiguration<ModelInference>
    {
        public void Configure(EntityTypeBuilder<ModelInference> builder)
        {
            builder.HasKey(i => i.Id);
            
            builder.HasOne(i => i.Frame)
                   .WithMany()
                   .HasForeignKey(i => i.FrameId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.Model)
                   .WithMany()
                   .HasForeignKey(i => i.ModelId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(i => i.DetectedClass).HasMaxLength(150).IsRequired();
            builder.Property(i => i.ConfidenceScore).HasColumnType("numeric(5,4)");
            builder.Property(i => i.InferenceLatencyMs).HasColumnType("numeric(10,3)");
            
            builder.OwnsOne(i => i.BoundingBox, bb =>
            {
                bb.ToJson();
            });

            builder.HasIndex(i => i.FrameId);
            builder.HasIndex(i => new { i.ModelId, i.CreatedAt });
        }
    }
}
