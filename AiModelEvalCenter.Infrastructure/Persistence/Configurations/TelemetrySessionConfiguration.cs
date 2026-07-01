using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiModelEvalCenter.Infrastructure.Persistence.Configurations
{
    public class TelemetrySessionConfiguration : IEntityTypeConfiguration<TelemetrySession>
    {
        public void Configure(EntityTypeBuilder<TelemetrySession> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.AircraftId).HasMaxLength(100).IsRequired();
            builder.Property(s => s.SessionLabel).HasMaxLength(200);
            builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        }
    }
}
