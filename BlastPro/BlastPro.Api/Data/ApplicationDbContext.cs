using BlastPro.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<BlastProject> BlastProjects => Set<BlastProject>();
    public DbSet<BlastHole> BlastHoles => Set<BlastHole>();
    public DbSet<ExplosiveProduct> ExplosiveProducts => Set<ExplosiveProduct>();
    public DbSet<CalculationResult> CalculationResults => Set<CalculationResult>();
    public DbSet<BlastWarning> BlastWarnings => Set<BlastWarning>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCompany(modelBuilder);
        ConfigureApplicationUser(modelBuilder);
        ConfigureBlastProject(modelBuilder);
        ConfigureBlastHole(modelBuilder);
        ConfigureExplosiveProduct(modelBuilder);
        ConfigureCalculationResult(modelBuilder);
        ConfigureBlastWarning(modelBuilder);

        DatabaseSeeder.Seed(modelBuilder);
    }

    private static void ConfigureCompany(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Company>();

        entity.Property(company => company.Name).HasMaxLength(150).IsRequired();
        entity.Property(company => company.RegistrationNumber).HasMaxLength(80);
        entity.Property(company => company.ContactEmail).HasMaxLength(254);
        entity.Property(company => company.ContactPhone).HasMaxLength(30);
        entity.Property(company => company.Address).HasMaxLength(300);
        entity.Property(company => company.IsActive).HasDefaultValue(true);
        entity.Property(company => company.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(company => company.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasIndex(company => company.Name);
        entity.HasIndex(company => company.RegistrationNumber)
            .IsUnique()
            .HasFilter("[RegistrationNumber] IS NOT NULL");
    }

    private static void ConfigureApplicationUser(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ApplicationUser>();

        entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
        entity.Property(user => user.NickName).HasMaxLength(100);
        entity.Property(user => user.Gender).HasMaxLength(50);
        entity.Property(user => user.Country).HasMaxLength(100);
        entity.Property(user => user.TimeZoneId).HasMaxLength(100);
        entity.Property(user => user.CertificationId).HasMaxLength(80);
        entity.Property(user => user.IsActive).HasDefaultValue(true);
        entity.Property(user => user.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(user => user.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL");
        entity.HasIndex(user => new { user.CompanyId, user.CertificationId })
            .IsUnique()
            .HasFilter("[CertificationId] IS NOT NULL");

        entity.HasOne(user => user.Company)
            .WithMany(company => company.Users)
            .HasForeignKey(user => user.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBlastProject(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BlastProject>();

        entity.Property(project => project.Name).HasMaxLength(150).IsRequired();
        entity.Property(project => project.SiteLocation).HasMaxLength(250).IsRequired();
        entity.Property(project => project.BlastType).HasMaxLength(100).IsRequired();
        entity.Property(project => project.RockType).HasMaxLength(100);
        entity.Property(project => project.RockDensity).HasPrecision(18, 4);
        entity.Property(project => project.Burden).HasPrecision(18, 4);
        entity.Property(project => project.Spacing).HasPrecision(18, 4);
        entity.Property(project => project.VibrationThreshold).HasPrecision(18, 4);
        entity.Property(project => project.Status).HasConversion<int>();
        entity.Property(project => project.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(project => project.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(project => project.RowVersion).IsRowVersion();

        entity.HasQueryFilter(project => !project.IsDeleted);
        entity.HasIndex(project => new { project.CompanyId, project.OwnerId, project.IsDeleted });
        entity.HasIndex(project => new { project.CompanyId, project.Status });

        entity.HasOne(project => project.Company)
            .WithMany(company => company.Projects)
            .HasForeignKey(project => project.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(project => project.Owner)
            .WithMany(user => user.OwnedProjects)
            .HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(project => project.ExplosiveProduct)
            .WithMany(product => product.Projects)
            .HasForeignKey(project => project.ExplosiveProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_BlastProjects_RockDensity_Positive",
                "[RockDensity] IS NULL OR [RockDensity] > 0");
            table.HasCheckConstraint(
                "CK_BlastProjects_Burden_Positive",
                "[Burden] IS NULL OR [Burden] > 0");
            table.HasCheckConstraint(
                "CK_BlastProjects_Spacing_Positive",
                "[Spacing] IS NULL OR [Spacing] > 0");
            table.HasCheckConstraint(
                "CK_BlastProjects_VibrationThreshold_Positive",
                "[VibrationThreshold] IS NULL OR [VibrationThreshold] > 0");
        });
    }

    private static void ConfigureBlastHole(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BlastHole>();

        entity.Property(hole => hole.XCoordinate).HasPrecision(18, 4);
        entity.Property(hole => hole.YCoordinate).HasPrecision(18, 4);
        entity.Property(hole => hole.Depth).HasPrecision(18, 4);
        entity.Property(hole => hole.ChargeKg).HasPrecision(18, 4);
        entity.Property(hole => hole.StemmingMetres).HasPrecision(18, 4);
        entity.Property(hole => hole.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(hole => hole.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasQueryFilter(hole => !hole.BlastProject.IsDeleted);
        entity.HasIndex(hole => new { hole.BlastProjectId, hole.HoleNumber }).IsUnique();

        entity.HasOne(hole => hole.BlastProject)
            .WithMany(project => project.Holes)
            .HasForeignKey(hole => hole.BlastProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(hole => hole.ExplosiveProduct)
            .WithMany(product => product.Holes)
            .HasForeignKey(hole => hole.ExplosiveProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint("CK_BlastHoles_HoleNumber_Positive", "[HoleNumber] > 0");
            table.HasCheckConstraint("CK_BlastHoles_Depth_Positive", "[Depth] > 0");
            table.HasCheckConstraint("CK_BlastHoles_Charge_NonNegative", "[ChargeKg] >= 0");
            table.HasCheckConstraint("CK_BlastHoles_Stemming_NonNegative", "[StemmingMetres] >= 0");
            table.HasCheckConstraint("CK_BlastHoles_Delay_NonNegative", "[DelayMilliseconds] >= 0");
        });
    }

    private static void ConfigureExplosiveProduct(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ExplosiveProduct>();

        entity.Property(product => product.Name).HasMaxLength(150).IsRequired();
        entity.Property(product => product.PricePerKg).HasPrecision(18, 4);
        entity.Property(product => product.CurrencyCode).HasMaxLength(3).IsRequired();
        entity.Property(product => product.IsActive).HasDefaultValue(true);
        entity.Property(product => product.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(product => product.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasIndex(product => new { product.CompanyId, product.Name }).IsUnique();

        entity.HasOne(product => product.Company)
            .WithMany(company => company.ExplosiveProducts)
            .HasForeignKey(product => product.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
            table.HasCheckConstraint("CK_ExplosiveProducts_Price_NonNegative", "[PricePerKg] >= 0"));
    }

    private static void ConfigureCalculationResult(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CalculationResult>();

        entity.Property(result => result.TotalExplosiveKg).HasPrecision(18, 4);
        entity.Property(result => result.TotalDrillingMetres).HasPrecision(18, 4);
        entity.Property(result => result.EstimatedVolumeCubicMetres).HasPrecision(18, 4);
        entity.Property(result => result.EstimatedTonnageTonnes).HasPrecision(18, 4);
        entity.Property(result => result.TotalCost).HasPrecision(18, 2);
        entity.Property(result => result.CurrencyCode).HasMaxLength(3);
        entity.Property(result => result.MaxChargePerDelayKg).HasPrecision(18, 4);
        entity.Property(result => result.PowderFactorKgPerTonne).HasPrecision(18, 6);
        entity.Property(result => result.PredictedPpvMmPerSecond).HasPrecision(18, 4);
        entity.Property(result => result.PredictedFlyrockMetres).HasPrecision(18, 4);
        entity.Property(result => result.CalculatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasQueryFilter(result => !result.BlastProject.IsDeleted);
        entity.HasIndex(result => new { result.BlastProjectId, result.IsCurrent })
            .IsUnique()
            .HasFilter("[IsCurrent] = 1");
        entity.HasIndex(result => result.CalculatedAtUtc);

        entity.HasOne(result => result.BlastProject)
            .WithMany(project => project.CalculationResults)
            .HasForeignKey(result => result.BlastProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(result => result.CalculatedByUser)
            .WithMany(user => user.CalculationResults)
            .HasForeignKey(result => result.CalculatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table =>
        {
            table.HasCheckConstraint("CK_CalculationResults_TotalHoles_NonNegative", "[TotalHoles] >= 0");
            table.HasCheckConstraint("CK_CalculationResults_TotalExplosive_NonNegative", "[TotalExplosiveKg] >= 0");
            table.HasCheckConstraint("CK_CalculationResults_TotalDrilling_NonNegative", "[TotalDrillingMetres] >= 0");
            table.HasCheckConstraint("CK_CalculationResults_TotalCost_NonNegative", "[TotalCost] >= 0");
            table.HasCheckConstraint("CK_CalculationResults_MaxCharge_NonNegative", "[MaxChargePerDelayKg] >= 0");
            table.HasCheckConstraint(
                "CK_CalculationResults_PredictedFlyrock_NonNegative",
                "[PredictedFlyrockMetres] IS NULL OR [PredictedFlyrockMetres] >= 0");
        });
    }

    private static void ConfigureBlastWarning(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BlastWarning>();

        entity.Property(warning => warning.Severity).HasConversion<int>();
        entity.Property(warning => warning.Code).HasMaxLength(80).IsRequired();
        entity.Property(warning => warning.Message).HasMaxLength(600).IsRequired();
        entity.Property(warning => warning.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        entity.HasQueryFilter(warning => !warning.CalculationResult.BlastProject.IsDeleted);
        entity.HasIndex(warning => new { warning.CalculationResultId, warning.Severity });

        entity.HasOne(warning => warning.CalculationResult)
            .WithMany(result => result.Warnings)
            .HasForeignKey(warning => warning.CalculationResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
