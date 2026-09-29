
using System.Security.Claims;
using BlastPro.Api.Controllers;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Tests.Integration;

public sealed class ProjectAccessTests
{
    [Fact]
    public async Task Pattern_design_loads_project_holes_and_company_products()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var response = await controller.GetPatternDesign(data.ProjectId);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var design = Assert.IsType<PatternDesignDto>(ok.Value);
        Assert.Equal(data.ProjectId, design.ProjectId);
        Assert.Equal(2, design.Holes.Count);
        Assert.Single(design.ExplosiveProducts);
        Assert.Contains(design.AeciProducts, product => product.Code == "S300-VOLCANO" &&
            product.Name == "S300 Volcano" && product.EnergyMinMjPerKg == 2.00m &&
            product.Application.Contains("Geothermal"));
    }

    [Fact]
    public async Task Incomplete_pattern_can_be_saved_and_reopened_as_a_draft()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var response = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite",
            BenchLengthMetres = 24m,
            LayoutRows = 3,
            TimingOrder = "Sequential"
        });

        var saved = Assert.IsType<PatternDesignDto>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(24m, saved.BenchLengthMetres);
        Assert.Null(saved.BenchWidthMetres);
        Assert.Equal(0m, saved.RockDensity);
        Assert.Equal(0m, saved.VibrationThreshold);
        Assert.Empty(saved.Holes);
        var reopened = Assert.IsType<PatternDesignDto>(Assert.IsType<OkObjectResult>(
            (await controller.GetPatternDesign(data.ProjectId)).Result).Value);
        Assert.Equal(24m, reopened.BenchLengthMetres);
        Assert.Equal("Sequential", reopened.TimingOrder);
    }

    [Theory]
    [InlineData("Chevron")]
    [InlineData("Echelon")]
    [InlineData("HalfRowOverlap")]
    public async Task New_timing_sequences_can_be_saved_and_reopened(string timingOrder)
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var response = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite",
            TimingOrder = timingOrder,
            TimingIntervalMilliseconds = 25
        });

        Assert.IsType<OkObjectResult>(response.Result);
        var reopened = Assert.IsType<PatternDesignDto>(Assert.IsType<OkObjectResult>(
            (await controller.GetPatternDesign(data.ProjectId)).Result).Value);
        Assert.Equal(timingOrder, reopened.TimingOrder);
        Assert.Equal(25, reopened.TimingIntervalMilliseconds);
    }

    [Fact]
    public async Task Pattern_design_save_adds_updates_and_removes_holes()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var saved = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite",
            RockDensity = 2.7m,
            Burden = 3.2m,
            Spacing = 3.8m,
            BenchLengthMetres = 18m,
            BenchWidthMetres = 12m,
            PatternType = "Staggered",
            ReferenceExplosiveFamily = "S100",
            VibrationThreshold = 9m,
            Holes =
            [
                new PatternHoleDto
                {
                    Id = data.FirstHoleId,
                    Number = 1,
                    X = 1,
                    Y = 2,
                    Depth = 13,
                    DiameterMillimetres = 115m,
                    SubdrillMetres = 1.15m,
                    ExplosiveProductId = data.ProductId,
                    Charge = 8,
                    Stemming = 3,
                    Delay = 25
                },
                new PatternHoleDto
                {
                    Number = 2,
                    X = 4,
                    Y = 5,
                    Depth = 12,
                    ExplosiveProductId = data.ProductId,
                    Charge = 7,
                    Stemming = 2,
                    Delay = 50
                }
            ]
        });

        var ok = Assert.IsType<OkObjectResult>(saved.Result);
        var design = Assert.IsType<PatternDesignDto>(ok.Value);
        Assert.Equal(2, design.Holes.Count);
        Assert.Equal(13, design.Holes.Single(h => h.Number == 1).Depth);
        Assert.DoesNotContain(design.Holes, h => h.Id == data.SecondHoleId);
        Assert.Equal(2.7m, design.RockDensity);
        Assert.Equal(18m, design.BenchLengthMetres);
        Assert.Equal(12m, design.BenchWidthMetres);
        Assert.Equal("Staggered", design.PatternType);
        Assert.Equal("S100", design.ReferenceExplosiveFamily);
        Assert.Equal(115m, design.Holes.Single(h => h.Number == 1).DiameterMillimetres);
        Assert.Equal(1.15m, design.Holes.Single(h => h.Number == 1).SubdrillMetres);
    }

    [Fact]
    public async Task Selected_aeci_products_and_individual_loading_densities_survive_reopening()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var response = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite", DefaultAeciProductCode = "S100",
            Holes =
            [
                new PatternHoleDto { Number = 1, Depth = 10, Stemming = 2,
                    AeciProductCode = "S100", ProductDensityGramsPerCc = 1.12m, Charge = 75 },
                new PatternHoleDto { Number = 2, Depth = 11, Stemming = 2.5m,
                    AeciProductCode = "S300-VOLCANO", ProductDensityGramsPerCc = 1.18m, Charge = 80 }
            ]
        });

        Assert.IsType<OkObjectResult>(response.Result);
        var reopened = Assert.IsType<PatternDesignDto>(Assert.IsType<OkObjectResult>(
            (await controller.GetPatternDesign(data.ProjectId)).Result).Value);
        Assert.Equal("S100", reopened.DefaultAeciProductCode);
        Assert.Equal("S100", reopened.Holes.Single(h => h.Number == 1).AeciProductCode);
        Assert.Equal(1.12m, reopened.Holes.Single(h => h.Number == 1).ProductDensityGramsPerCc);
        Assert.Equal("S300-VOLCANO", reopened.Holes.Single(h => h.Number == 2).AeciProductCode);
        Assert.Equal(1.18m, reopened.Holes.Single(h => h.Number == 2).ProductDensityGramsPerCc);
    }

    [Fact]
    public async Task Automatic_residential_limit_is_recomputed_on_the_server()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var saved = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite",
            RockDensity = 2.7m,
            Burden = 3m,
            Spacing = 4m,
            VibrationThreshold = 1m,
            ReceptorStructureType = "ResidentialDrywall",
            DominantFrequencyHz = 10m,
            VibrationThresholdMode = "Automatic"
        });

        var result = Assert.IsType<PatternDesignDto>(Assert.IsType<OkObjectResult>(saved.Result).Value);
        Assert.Equal(19.05m, result.VibrationThreshold);
        Assert.Equal("Automatic", result.VibrationThresholdMode);
    }

    [Fact]
    public async Task Pattern_design_rejects_product_from_another_company()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var otherProduct = new ExplosiveProduct
        {
            Id = 99,
            CompanyId = 99,
            Name = "Other product",
            PricePerKg = 1,
            IsActive = true
        };
        db.ExplosiveProducts.Add(otherProduct);
        await db.SaveChangesAsync();
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var response = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite",
            RockDensity = 2.7m,
            Burden = 3,
            Spacing = 3,
            VibrationThreshold = 10,
            Holes =
            [
                new PatternHoleDto
                {
                    Number = 1,
                    Depth = 10,
                    ExplosiveProductId = otherProduct.Id,
                    Charge = 5,
                    Stemming = 2
                }
            ]
        });

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task Blaster_cannot_open_another_users_pattern_design()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, "different-user", data.CompanyId);

        var response = await controller.GetPatternDesign(data.ProjectId);

        Assert.IsType<NotFoundResult>(response.Result);
    }

    [Fact]
    public async Task Pattern_design_rejects_duplicate_hole_numbers()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var response = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite",
            RockDensity = 2.7m,
            Burden = 3,
            Spacing = 3,
            VibrationThreshold = 10,
            Holes =
            [
                new PatternHoleDto { Number = 1, Depth = 10, Stemming = 2 },
                new PatternHoleDto { Number = 1, Depth = 11, Stemming = 2 }
            ]
        });

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task Pattern_design_rejects_an_outdated_row_version()
    {
        await using var db = CreateDatabase();
        var data = await SeedPatternDesign(db);
        var project = await db.BlastProjects.SingleAsync(p => p.Id == data.ProjectId);
        project.RowVersion = [1, 2, 3];
        await db.SaveChangesAsync();
        var controller = CreateController(db, data.UserId, data.CompanyId);

        var response = await controller.SavePatternDesign(data.ProjectId, new SavePatternDesignDto
        {
            RockType = "Granite",
            RockDensity = 2.7m,
            Burden = 3,
            Spacing = 3,
            VibrationThreshold = 10,
            RowVersion = Convert.ToBase64String([9, 9, 9])
        });

        Assert.IsType<ConflictObjectResult>(response.Result);
    }

    private static ApplicationDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ProjectsController CreateController(
        ApplicationDbContext db, string userId, int companyId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("companyId", companyId.ToString())
        ], "Test");

        return new ProjectsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static async Task<SeedData> SeedPatternDesign(ApplicationDbContext db)
    {
        var company = new Company { Id = 1, Name = "Test Company" };
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@test.local",
            FullName = "Test User",
            CompanyId = company.Id
        };
        var product = new ExplosiveProduct
        {
            Id = 1,
            CompanyId = company.Id,
            Name = "Test Product",
            PricePerKg = 10,
            IsActive = true
        };
        var project = new BlastProject
        {
            Id = 1,
            CompanyId = company.Id,
            OwnerId = user.Id,
            Name = "Test Project",
            SiteLocation = "Test Site",
            BlastType = "Surface",
            Status = ProjectStatus.Draft
        };
        var firstHole = new BlastHole
        {
            BlastProjectId = project.Id,
            HoleNumber = 1,
            Depth = 10,
            ChargeKg = 5,
            StemmingMetres = 2,
            DelayMilliseconds = 25
        };
        var secondHole = new BlastHole
        {
            BlastProjectId = project.Id,
            HoleNumber = 2,
            Depth = 11,
            ChargeKg = 6,
            StemmingMetres = 2,
            DelayMilliseconds = 50
        };

        db.AddRange(company, user, product, project, firstHole, secondHole);
        await db.SaveChangesAsync();

        return new SeedData(
            company.Id,
            user.Id,
            project.Id,
            product.Id,
            firstHole.Id,
            secondHole.Id);
    }

    private sealed record SeedData(
        int CompanyId,
        string UserId,
        int ProjectId,
        int ProductId,
        int FirstHoleId,
        int SecondHoleId);
}
