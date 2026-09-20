using System.Text;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services;
using BlastPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BlastPro.Api.Models.Entities;  

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Database
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------------------------------------------------------------------------
// 2. Identity
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ---------------------------------------------------------------------------
// 3. JWT
// ---------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key missing from configuration.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BlastPro.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BlastPro.Clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// 4. Application services
// ---------------------------------------------------------------------------
// Uncomment the services as you create them.
// builder.Services.AddScoped<IProjectService, ProjectService>();
// builder.Services.AddScoped<ICalculationService, CalculationService>();
// builder.Services.AddScoped<IBlasterService, BlasterService>();
// builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ---------------------------------------------------------------------------
// 5. Controllers + OpenAPI (.NET 10 built-in)
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// ---------------------------------------------------------------------------
// 6. CORS
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlastProClients", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// 7. Middleware
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();      // JSON spec (already there)
    app.UseSwagger();      // serves /swagger/v1/swagger.json
    app.UseSwaggerUI();    // serves /swagger/index.html  ← visual UI
}

app.UseHttpsRedirection();
app.UseCors("BlastProClients");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ---------------------------------------------------------------------------
// 8. Migrate + seed
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await db.Database.MigrateAsync();

    // 1. Roles
    foreach (var role in new[] { "MainCompanyUser", "Blaster" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // 2. Company
    var company = await db.Companies.FirstOrDefaultAsync(c => c.Name == "Xploma");
    if (company is null)
    {
        company = new Company
        {
            Name = "Xploma",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
    }

    // 3. Admin user — attached to the company
    const string adminEmail = "admin@xploma.co.za";
    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            IsActive = true,
            FullName = "Xploma Admin",
            CompanyId = company.Id
        };

        var result = await userManager.CreateAsync(admin, "Admin@12345!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "MainCompanyUser");
    }
}

app.Run();