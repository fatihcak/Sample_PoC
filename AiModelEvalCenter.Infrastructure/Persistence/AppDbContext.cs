using AiModelEvalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiModelEvalCenter.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AiModel> AiModels => Set<AiModel>();
        public DbSet<TelemetrySession> TelemetrySessions => Set<TelemetrySession>();
        public DbSet<TelemetryFrame> TelemetryFrames => Set<TelemetryFrame>();
        public DbSet<ModelInference> ModelInferences => Set<ModelInference>();
        public DbSet<GroundTruth> GroundTruths => Set<GroundTruth>();
        public DbSet<DriftMetric> DriftMetrics => Set<DriftMetric>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
