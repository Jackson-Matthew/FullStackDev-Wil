using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Data;

public static class DatabaseSeeder
{
    public const string MainCompanyUserRole = "MainCompanyUser";
    public const string BlasterRole = "Blaster";

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "8f720302-546d-475f-b14d-28eef686a101",
                Name = MainCompanyUserRole,
                NormalizedName = MainCompanyUserRole.ToUpperInvariant(),
                ConcurrencyStamp = "role-main-company-user-v1"
            },
            new IdentityRole
            {
                Id = "4a5c762b-41ba-4d2a-bbae-041d24ffab02",
                Name = BlasterRole,
                NormalizedName = BlasterRole.ToUpperInvariant(),
                ConcurrencyStamp = "role-blaster-v1"
            });
    }
}
