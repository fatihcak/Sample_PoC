using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiModelEvalCenter.Infrastructure.Persistence.Configurations
{
    public class TelemetryFrameConfiguration : IEntityTypeConfiguration<TelemetryFrame>
    {
        public void Configure(EntityTypeBuilder<TelemetryFrame> builder)
        {
            builder.HasKey(f => f.Id);
            
            builder.HasOne(f => f.Session)
                   .WithMany()
                   .HasForeignKey(f => f.SessionId)
                   .OnDelete(DeleteBehavior.Cascade);
            
            builder.Property(f => f.AltitudeM).HasColumnType("numeric(10,2)");
            builder.Property(f => f.VelocityMps).HasColumnType("numeric(10,2)");
            builder.Property(f => f.HeadingDeg).HasColumnType("numeric(6,2)");

            builder.HasIndex(f => new { f.SessionId, f.FrameSequence }).IsUnique();
            builder.HasIndex(f => new { f.SessionId, f.CapturedAt });
        }
    }
}
