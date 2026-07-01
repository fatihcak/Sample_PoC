using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace AiModelEvalCenter.Infrastructure.Persistence.Configurations
{
    public class AiModelConfiguration : IEntityTypeConfiguration<AiModel>
    {
        public void Configure(EntityTypeBuilder<AiModel> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
            builder.Property(m => m.Version).HasMaxLength(50).IsRequired();
            builder.Property(m => m.ModelType).HasMaxLength(100).IsRequired();
            
            builder.HasIndex(m => new { m.Name, m.Version }).IsUnique();

            // Seed Data for Phase 1
            builder.HasData(
                new AiModel
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "YOLOv8-Aerial",
                    Version = "2.3.1",
                    ModelType = "object_detection",
                    Framework = "ONNX",
                    Description = "Mock Object Detection Model",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new AiModel
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "RT-DETR-Tactical",
                    Version = "1.0.0",
                    ModelType = "object_detection",
                    Framework = "TensorRT",
                    Description = "High precision tactical model",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new AiModel
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Thermal-Seg",
                    Version = "4.0",
                    ModelType = "segmentation",
                    Framework = "PyTorch",
                    Description = "Thermal segmentation model",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
                }
            );
        }
    }
}
